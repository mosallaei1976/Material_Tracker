// Publish first, then run: node tests/pwa-checks.cjs <playwright-module-path> [publish-wwwroot]
const { chromium } = require(process.argv[2] || 'playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const http = require('node:http');
const path = require('node:path');

const root = path.resolve(process.argv[3] || 'tests/artifacts/pwa-publish/wwwroot');
const types = { '.html': 'text/html', '.js': 'text/javascript', '.json': 'application/json',
    '.webmanifest': 'application/manifest+json', '.wasm': 'application/wasm',
    '.css': 'text/css', '.png': 'image/png', '.woff2': 'font/woff2' };

(async () => {
    // Emulate the deployment workflow's base-href rewrite after publishing.
    const server = http.createServer(async (request, response) => {
        try {
            const pathname = decodeURIComponent(new URL(request.url, 'http://localhost').pathname);
            const base = pathname.startsWith('/Material_Tracker/') ? '/Material_Tracker/' : '/';
            const relative = pathname.slice(base.length) || 'index.html';
            const file = path.resolve(root, relative);
            if (!file.startsWith(root + path.sep)) {
                response.writeHead(403).end();
                return;
            }
            let body = await fs.readFile(file);
            if (relative === 'index.html') {
                body = Buffer.from(body.toString().replace('<base href="/" />', `<base href="${base}" />`));
            }
            response.writeHead(200, { 'Content-Type': types[path.extname(file)] || 'application/octet-stream' });
            response.end(body);
        } catch {
            response.writeHead(404).end();
        }
    });
    let browser;
    try {
        await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
        const origin = `http://127.0.0.1:${server.address().port}`;
        browser = await chromium.launch({ channel: 'msedge', headless: true });
        for (const base of ['/', '/Material_Tracker/']) {
            const context = await browser.newContext();
            const page = await context.newPage();
            page.setDefaultTimeout(60000);
            const errors = [];
            page.on('pageerror', error => errors.push(error.message));
            page.on('console', message => {
                if (message.type() === 'error' || message.type() === 'warning') console.error(message.text());
            });
            await page.goto(origin + base);
            await page.getByRole('heading', { name: 'از یک پروژه شروع کنید' }).waitFor();
            await page.waitForFunction(() => navigator.serviceWorker.controller !== null);
            const registration = await page.evaluate(async () => ({
                scope: (await navigator.serviceWorker.ready).scope,
                manifest: await fetch(document.querySelector('link[rel="manifest"]').href).then(r => r.json())
            }));
            assert.equal(registration.scope, origin + base);
            assert.equal(registration.manifest.display, 'standalone');
            for (const icon of registration.manifest.icons) {
                const dimensions = await page.evaluate(async src => {
                    const img = new Image();
                    img.src = src;
                    await img.decode();
                    return `${img.naturalWidth}x${img.naturalHeight}`;
                }, icon.src);
                assert.equal(dimensions, icon.sizes);
            }
            await page.getByRole('navigation').getByRole('link', { name: 'پروژه‌ها و پشتیبان', exact: true }).click();
            await page.locator('#project-name').fill('پروژه آنلاین');
            await page.getByRole('button', { name: 'ساخت پروژه', exact: true }).click();
            await page.getByRole('status').filter({ hasText: 'پروژه ذخیره شد' }).waitFor();
            const before = await page.evaluate(() => localStorage.getItem('sitepack:data:v1'));
            await context.setOffline(true);
            // A fresh document on a deep route must load the runtime and local font offline.
            await page.reload();
            await page.locator('#project-name').waitFor();
            assert.equal(await page.evaluate(() => localStorage.getItem('sitepack:data:v1')), before);
            assert.equal(await page.evaluate(async () => {
                await document.fonts.ready;
                return document.fonts.check('16px Vazirmatn');
            }), true);
            await page.locator('#project-name').fill('پروژه آفلاین');
            await page.getByRole('button', { name: 'ساخت پروژه', exact: true }).click();
            await page.getByRole('status').filter({ hasText: 'پروژه ذخیره شد' }).waitFor();
            await page.reload();
            await page.locator('#project-name').waitFor();
            assert.equal(await page.evaluate(() => JSON.parse(localStorage.getItem('sitepack:data:v1')).Projects.length), 2);
            assert.deepEqual(errors, []);
            console.log(`PASS ${base}: manifest, PNG sizes, worker scope, offline deep-route startup, font, persistence and writes`);
            await context.close();
        }
    } finally {
        if (browser) await browser.close();
        await new Promise(resolve => server.close(resolve));
    }
})().catch(error => { console.error(error); process.exitCode = 1; });
