const fs = require("fs");
const { spawn } = require("child_process");

const browserPath = process.argv[2];
const screenshotPath = process.argv[3];
const username = process.env.CRM_TEST_USER;
const password = process.env.CRM_TEST_PASSWORD;

if (!browserPath || !screenshotPath || !username || !password) {
    throw new Error("Thiếu trình duyệt, screenshot hoặc thông tin đăng nhập từ biến môi trường.");
}

const delay = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));

async function waitUntil(callback, timeoutMilliseconds, description) {
    const startedAt = Date.now();
    while (Date.now() - startedAt < timeoutMilliseconds) {
        const result = await callback();
        if (result) return result;
        await delay(250);
    }
    throw new Error(`Hết thời gian chờ: ${description}`);
}

async function main() {
    const browser = spawn(browserPath, ["--headless=new", "--disable-gpu", "--no-sandbox", "--remote-debugging-pipe", "about:blank"], {
        stdio: ["ignore", "ignore", "inherit", "pipe", "pipe"],
        windowsHide: true
    });
    const pending = new Map();
    let sequence = 0;
    let responseBuffer = "";
    browser.stdio[4].setEncoding("utf8");
    browser.stdio[4].on("data", chunk => {
        responseBuffer += chunk;
        let separatorIndex;
        while ((separatorIndex = responseBuffer.indexOf("\0")) >= 0) {
            const rawMessage = responseBuffer.slice(0, separatorIndex);
            responseBuffer = responseBuffer.slice(separatorIndex + 1);
            if (!rawMessage) continue;
            const message = JSON.parse(rawMessage);
            if (!message.id || !pending.has(message.id)) continue;
            const promise = pending.get(message.id);
            pending.delete(message.id);
            if (message.error) promise.reject(new Error(message.error.message));
            else promise.resolve(message.result);
        }
    });

    function sendCommand(method, params = {}, sessionId) {
        return new Promise((resolve, reject) => {
            const id = ++sequence;
            pending.set(id, { resolve, reject });
            browser.stdio[3].write(JSON.stringify({ id, method, params, sessionId }) + "\0");
        });
    }

    const target = await sendCommand("Target.createTarget", { url: "about:blank" });
    const attached = await sendCommand("Target.attachToTarget", { targetId: target.targetId, flatten: true });
    const send = (method, params = {}) => sendCommand(method, params, attached.sessionId);
    async function evaluate(expression) {
        const result = await send("Runtime.evaluate", { expression, awaitPromise: true, returnByValue: true });
        if (result.exceptionDetails) throw new Error(result.exceptionDetails.text || "JavaScript runtime error");
        return result.result.value;
    }

    await send("Page.enable");
    await send("Runtime.enable");
    await send("Emulation.setDeviceMetricsOverride", { width: 1900, height: 1000, deviceScaleFactor: 1, mobile: false });
    await send("Page.navigate", { url: "http://crm.git/Cate/ReviewBatchItem/Index" });
    await waitUntil(() => evaluate("Boolean(document.querySelector('#UserName'))"), 20000, "form đăng nhập");
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

    await waitUntil(() => evaluate("location.pathname.indexOf('/Cate/ReviewBatchItem/Index') >= 0 && Boolean(document.querySelector('#DSDigitalSalesReview'))"), 30000, "trang danh sách rà soát");
    await waitUntil(() => evaluate("window.jQuery && $.fn.DataTable && $.fn.DataTable.isDataTable('#DSDigitalSalesReview')"), 20000, "DataTable rà soát");
    await waitUntil(() => evaluate("Boolean($('#DSDigitalSalesReview').DataTable().ajax.json())"), 20000, "dữ liệu rà soát ban đầu");

    await evaluate(`(() => {
        const $batch = $('#ReviewDigitalSalesBatchID');
        if (!$batch.val()) {
            const firstValue = $batch.find('option').map(function () { return $(this).val(); }).get().find(value => Number(value) > 0);
            if (firstValue) $batch.val(firstValue).trigger('chosen:updated');
        }
        localStorage.removeItem('review_digital_sales_table_state');
    })()`);

    async function applyReviewedFilter(value) {
        const radioValue = value ? "True" : "False";
        await evaluate(`(() => {
            const table = $('#DSDigitalSalesReview').DataTable();
            window.__reviewDigitalSalesXhrDone = false;
            table.one('xhr.dt.runtimeReviewTest', function () {
                window.__reviewDigitalSalesXhrDone = true;
            });
            $('input[name="IsReviewed"][value="${radioValue}"]').prop('checked', true);
            searchReviewDigitalSales();
        })()`);
        await waitUntil(() => evaluate("window.__reviewDigitalSalesXhrDone === true"), 20000, `lọc IsReviewed=${value}`);
        await delay(300);
        return evaluate(`(() => {
            const table = $('#DSDigitalSalesReview').DataTable();
            const json = table.ajax.json() || { data: [] };
            const params = table.ajax.params() || {};
            return {
                checkedValue: $('input[name="IsReviewed"]:checked').val(),
                sentValue: params.IsReviewed,
                rowCount: (json.data || []).length,
                allRowsMatch: (json.data || []).every(row => row.IsReviewed === ${value})
            };
        })()`);
    }

    const notReviewed = await applyReviewedFilter(false);
    const reviewed = await applyReviewedFilter(true);
    await applyReviewedFilter(false);

    const ui = await evaluate(`(() => {
        const headers = Array.from(document.querySelectorAll('#DSDigitalSalesReview thead th')).map(item => item.textContent.trim());
        const recordCell = document.querySelector('#DSDigitalSalesReview tbody tr td:nth-child(2)');
        const json = $('#DSDigitalSalesReview').DataTable().ajax.json() || { data: [] };
        return {
            headers,
            headerCount: headers.length,
            recordHeader: headers[1] || '',
            statusFilterExists: Boolean(document.querySelector('#ReviewDigitalSalesStatusID')),
            reviewedRadiosExist: document.querySelectorAll('input[name="IsReviewed"]').length === 2,
            recordHasTitleLink: Boolean(recordCell && recordCell.querySelector('a.sale-title')),
            recordHasStatusBadge: Boolean(recordCell && recordCell.querySelector('.sale-badge')),
            recordHasCodeBadge: Boolean(recordCell && recordCell.querySelector('.fa-hashtag')),
            responseHasDisplayFields: (json.data || []).every(row => Object.prototype.hasOwnProperty.call(row, 'StatusName')
                && Object.prototype.hasOwnProperty.call(row, 'ProductServiceNames')
                && Object.prototype.hasOwnProperty.call(row, 'IsKeyProject')
                && Object.prototype.hasOwnProperty.call(row, 'IsFollowed'))
        };
    })()`);

    const screenshot = await send("Page.captureScreenshot", { format: "png", captureBeyondViewport: false });
    fs.writeFileSync(screenshotPath, Buffer.from(screenshot.data, "base64"));

    const checks = {
        titleUpdated: ui.recordHeader === "H\u1ed3 s\u01a1 KD s\u1ea3n ph\u1ea9m DVS",
        sixColumnsOnly: ui.headerCount === 6,
        separateTypeColumnRemoved: ui.headers.indexOf("Lo\u1ea1i h\u00ecnh") < 0,
        separateStatusColumnRemoved: ui.headers.indexOf("Tr\u1ea1ng th\u00e1i") < 0,
        statusFilterPreserved: ui.statusFilterExists,
        reviewedFilterPreserved: ui.reviewedRadiosExist,
        recordRendererApplied: ui.recordHasTitleLink && ui.recordHasStatusBadge && ui.recordHasCodeBadge,
        responseHasDisplayFields: ui.responseHasDisplayFields,
        notReviewedFilterWorks: String(notReviewed.checkedValue).toLowerCase() === "false" && notReviewed.sentValue === false && notReviewed.allRowsMatch,
        reviewedFilterWorks: String(reviewed.checkedValue).toLowerCase() === "true" && reviewed.sentValue === true && reviewed.allRowsMatch
    };
    console.log(JSON.stringify({ checks, ui, notReviewed, reviewed, screenshotPath }, null, 2));
    if (Object.values(checks).some(value => value !== true)) process.exitCode = 1;
    browser.kill();
}

main().catch(error => {
    console.error(error.message);
    process.exit(1);
});
