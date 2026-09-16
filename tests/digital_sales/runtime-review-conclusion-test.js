const fs = require("fs");
const { spawn } = require("child_process");

const [browserPath, screenshotPath] = process.argv.slice(2);
const username = process.env.CRM_TEST_USER;
const password = process.env.CRM_TEST_PASSWORD;
if (!browserPath || !screenshotPath || !username || !password) process.exit(2);

const delay = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));

async function waitUntil(callback, timeout, description) {
    const started = Date.now();
    while (Date.now() - started < timeout) {
        const value = await callback();
        if (value) return value;
        await delay(250);
    }
    throw new Error("Timeout: " + description);
}

async function main() {
    const browser = spawn(browserPath, ["--headless=new", "--disable-gpu", "--no-sandbox", "--autoplay-policy=no-user-gesture-required", "--remote-debugging-pipe", "about:blank"], {
        stdio: ["ignore", "ignore", "inherit", "pipe", "pipe"],
        windowsHide: true
    });
    const pending = new Map();
    const runtimeErrors = [];
    let sequence = 0;
    let buffer = "";
    browser.stdio[4].setEncoding("utf8");
    browser.stdio[4].on("data", chunk => {
        buffer += chunk;
        let separator;
        while ((separator = buffer.indexOf("\0")) >= 0) {
            const raw = buffer.slice(0, separator);
            buffer = buffer.slice(separator + 1);
            if (!raw) continue;
            const message = JSON.parse(raw);
            if (!message.id) {
                if (message.method === "Runtime.exceptionThrown") {
                    const details = message.params.exceptionDetails || {};
                    runtimeErrors.push({
                        text: details.text || "Runtime exception",
                        description: details.exception && details.exception.description || "",
                        url: details.url || "",
                        line: details.lineNumber
                    });
                }
                continue;
            }
            const task = pending.get(message.id);
            if (!task) continue;
            pending.delete(message.id);
            if (message.error) task.reject(new Error(message.error.message));
            else task.resolve(message.result);
        }
    });

    function command(method, params = {}, sessionId) {
        return new Promise((resolve, reject) => {
            const id = ++sequence;
            pending.set(id, { resolve, reject });
            browser.stdio[3].write(JSON.stringify({ id, method, params, sessionId }) + "\0");
        });
    }

    try {
        const target = await command("Target.createTarget", { url: "about:blank" });
        const attached = await command("Target.attachToTarget", { targetId: target.targetId, flatten: true });
        const send = (method, params = {}) => command(method, params, attached.sessionId);
        const evaluate = async expression => {
            const response = await send("Runtime.evaluate", { expression, awaitPromise: true, returnByValue: true });
            if (response.exceptionDetails) throw new Error(response.exceptionDetails.text || "Runtime error");
            return response.result.value;
        };

        await send("Page.enable");
        await send("Runtime.enable");
        await send("Emulation.setDeviceMetricsOverride", { width: 1900, height: 1000, deviceScaleFactor: 1, mobile: false });
        await send("Page.navigate", { url: "http://crm.git/Cate/ReviewBatchItem/Index" });
        await waitUntil(() => evaluate("Boolean(document.querySelector('#UserName'))"), 20000, "login form");
        await evaluate(`(() => {
            const setValue = (selector, value) => {
                const input = document.querySelector(selector);
                Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set.call(input, value);
                input.dispatchEvent(new Event('input', { bubbles: true }));
                input.dispatchEvent(new Event('change', { bubbles: true }));
            };
            setValue('#UserName', ${JSON.stringify(username)});
            setValue('#Password', ${JSON.stringify(password)});
            document.querySelector('#btnLogin').click();
        })()`);

        await waitUntil(() => evaluate("window.jQuery && $.fn.DataTable && $.fn.DataTable.isDataTable('#DSDigitalSalesReview')"), 30000, "review list");
        await waitUntil(() => evaluate("Boolean($('#DSDigitalSalesReview').DataTable().ajax.json())"), 20000, "review data");
        await evaluate(`(() => {
            const batch = $('#ReviewDigitalSalesBatchID option').map(function () { return $(this).val(); }).get().find(x => Number(x) > 0);
            $('#ReviewDigitalSalesBatchID').val(batch).trigger('chosen:updated');
            $('#ReviewDigitalSalesStatusID').val('');
            $('input[name="IsReviewed"][value="False"]').prop('checked', true);
            window.__conclusionReviewReloaded = false;
            $('#DSDigitalSalesReview').DataTable().one('xhr.dt.runtimeConclusionTest', function () { window.__conclusionReviewReloaded = true; });
            searchReviewDigitalSales();
        })()`);
        await waitUntil(() => evaluate("window.__conclusionReviewReloaded === true"), 20000, "filtered review data");
        await delay(300);
        const targetReview = await evaluate(`(() => {
            const batch = $('#ReviewDigitalSalesBatchID').val();
            const rows = ($('#DSDigitalSalesReview').DataTable().ajax.json() || { data: [] }).data || [];
            const row = rows[0];
            return row && Number(batch) > 0 ? { batchID: Number(batch), digitalSalesID: row.DigitalSalesID } : null;
        })()`);
        if (!targetReview) throw new Error("No DigitalSales review target");

        await send("Page.navigate", { url: `http://crm.git/Cate/DigitalSales/Detail/${targetReview.digitalSalesID}?reviewBatchID=${targetReview.batchID}` });
        await waitUntil(() => evaluate("Boolean(document.querySelector('#modal_ReviewBatch #ReviewConclusion'))"), 30000, "review conclusion form");

        const form = await evaluate(`(() => ({
            optionValues: Array.from(document.querySelectorAll('#ReviewConclusion option')).map(x => x.value),
            optionTexts: Array.from(document.querySelectorAll('#ReviewConclusion option')).map(x => x.textContent.trim()),
            confirmedChecked: document.querySelector('#IsConfirmed').checked,
            action: document.querySelector('form#ReviewBatch').getAttribute('action')
        }))()`);

        await evaluate(`(() => {
            $('#ReviewConclusion').val('');
            $('#IsConfirmed').prop('checked', true);
            $('#modal_ReviewBatch .js-review-batch-submit[data-continue="false"]').trigger('click');
        })()`);
        const validationText = await waitUntil(() => evaluate(`(() => {
            const item = document.querySelector('[data-valmsg-for="ReviewConclusion"]');
            return item && item.textContent.trim();
        })()`), 20000, "inline conclusion validation");
        const layout = await evaluate(`(() => {
            const body = document.querySelector('#modal_ReviewBatch .modal-body');
            const footer = document.querySelector('#modal_ReviewBatch .modal-footer');
            const validation = document.querySelector('[data-valmsg-for="ReviewConclusion"]');
            const rect = element => element ? ({ top: element.getBoundingClientRect().top, bottom: element.getBoundingClientRect().bottom }) : null;
            return {
                viewportHeight: window.innerHeight,
                bodyClientHeight: body && body.clientHeight,
                bodyScrollHeight: body && body.scrollHeight,
                footer: rect(footer),
                validation: rect(validation)
            };
        })()`);
        const screenshot = await send("Page.captureScreenshot", { format: "png", captureBeyondViewport: false });
        fs.writeFileSync(screenshotPath, Buffer.from(screenshot.data, "base64"));

        await send("Page.navigate", { url: `http://crm.git/Cate/ReviewReport/Index/${targetReview.batchID}` });
        await waitUntil(() => evaluate("window.jQuery && $.fn.DataTable && $.fn.DataTable.isDataTable('#DSReviewReport')"), 30000, "review report");
        await waitUntil(() => evaluate("Boolean($('#DSReviewReport').DataTable().ajax.json())"), 20000, "report data");
        const report = await evaluate(`(() => {
            const table = $('#DSReviewReport').DataTable();
            const json = table.ajax.json() || { data: [] };
            return {
                headers: Array.from(document.querySelectorAll('#DSReviewReport thead th')).map(x => x.textContent.trim()),
                rows: (json.data || []).map(x => ({ objectType: x.ObjectType, hasConclusionField: Object.prototype.hasOwnProperty.call(x, 'FinalReviewConclusion') })),
                digitalSalesLinks: Array.from(document.querySelectorAll('#DSReviewReport tbody a')).every(x => x.getAttribute('href').indexOf('/Cate/DigitalSales/Detail/') === 0)
            };
        })()`);

        const checks = {
            threeConclusionOptions: form.optionValues.filter(x => x).join(',') === '1,2,3',
            correctConclusionLabels: ['Ch\u1ea5p nh\u1eadn', 'Quan t\u00e2m', 'Kh\u00f4ng ch\u1ea5p nh\u1eadn'].every(x => form.optionTexts.includes(x)),
            confirmedByDefault: form.confirmedChecked === true,
            explicitFormAction: String(form.action || '').indexOf('/Cate/ReviewBatchItem/ReviewBatch') >= 0,
            inlineValidationWorks: String(validationText).length > 0,
            validationVisible: layout.validation && layout.validation.top >= 0 && layout.validation.bottom <= layout.viewportHeight,
            footerReachable: layout.footer && layout.footer.top < layout.viewportHeight,
            reportHasFinalConclusion: report.headers.includes('K\u1ebft lu\u1eadn cu\u1ed1i'),
            reportUsesDigitalSales: report.rows.every(x => x.objectType === 0 && x.hasConclusionField) && report.digitalSalesLinks,
            noRuntimeErrors: runtimeErrors.length === 0
        };
        console.log(JSON.stringify({ checks, form, validationText, layout, report, runtimeErrors, screenshotPath }, null, 2));
        if (Object.values(checks).some(x => x !== true)) process.exitCode = 1;
    }
    finally {
        browser.kill();
    }
}

main().catch(error => {
    console.error(error.message);
    process.exit(1);
});
