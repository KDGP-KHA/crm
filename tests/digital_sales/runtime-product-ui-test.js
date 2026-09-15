const fs = require("fs");
const { spawn } = require("child_process");

const browserPath = process.argv[2];
const screenshotPath = process.argv[3];
const username = process.env.CRM_TEST_USER;
const password = process.env.CRM_TEST_PASSWORD;

if (!browserPath || !screenshotPath || !username || !password) {
    throw new Error("Thiếu trình duyệt, đường dẫn screenshot hoặc thông tin đăng nhập từ biến môi trường.");
}

function delay(milliseconds) {
    return new Promise(resolve => setTimeout(resolve, milliseconds));
}

async function waitUntil(callback, timeoutMilliseconds, description) {
    const startedAt = Date.now();
    while (Date.now() - startedAt < timeoutMilliseconds) {
        const result = await callback();
        if (result) return result;
        await delay(200);
    }
    throw new Error(`Hết thời gian chờ: ${description}`);
}

async function main() {
    const browser = spawn(browserPath, [
        "--headless=new",
        "--disable-gpu",
        "--no-sandbox",
        "--remote-debugging-pipe",
        "about:blank"
    ], { stdio: ["ignore", "ignore", "inherit", "pipe", "pipe"], windowsHide: true });
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
    await send("Emulation.setDeviceMetricsOverride", { width: 1900, height: 900, deviceScaleFactor: 1, mobile: false });
    await send("Page.navigate", { url: "http://crm.git/Cate/DigitalSales/Detail/69" });

    await waitUntil(() => evaluate("Boolean(document.querySelector('#UserName'))"), 15000, "form đăng nhập");
    await evaluate(`(() => {
        const setValue = (selector, value) => {
            const input = document.querySelector(selector);
            const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set;
            setter.call(input, value);
            input.dispatchEvent(new Event('input', { bubbles: true }));
            input.dispatchEvent(new Event('change', { bubbles: true }));
        };
        setValue('#UserName', ${JSON.stringify(username)});
        setValue('#Password', ${JSON.stringify(password)});
        document.querySelector('#btnLogin').click();
    })()`);

    await waitUntil(() => evaluate("location.pathname.indexOf('/Cate/DigitalSales/Detail/69') >= 0 && Boolean(document.querySelector('#tab-products'))"), 30000, "trang chi tiết DigitalSales");
    await waitUntil(() => evaluate("typeof window.initDigitalSalesProductRuntimeForm === 'function'"), 15000, "JavaScript DigitalSalesDetail");

    const editButtonFound = await evaluate(`(() => {
        const button = document.querySelector('#tab-products button[onclick^="openEditProductModal"]');
        if (!button) return false;
        button.click();
        return true;
    })()`);
    if (!editButtonFound) throw new Error("Không tìm thấy nút chỉnh sửa dịch vụ để mở modal.");

    await waitUntil(() => evaluate("Boolean(document.querySelector('#modalProduct.show'))"), 15000, "modal sản phẩm");
    await waitUntil(() => evaluate("Boolean(document.querySelector('#modalProduct .select2-container'))"), 10000, "Select2 trong modal");
    await evaluate(`(() => {
        if (!document.querySelector('#productCostRows [data-detail-row="cost"]')) {
            document.querySelector('#modalProduct [data-add-row="cost"]').click();
        }
        if (!document.querySelector('#productRevenueRows [data-detail-row="revenue"]')) {
            document.querySelector('#modalProduct [data-add-row="revenue"]').click();
        }
    })()`);
    await waitUntil(() => evaluate("Boolean(document.querySelector('#productCostRows [data-detail-row=\"cost\"] .product-detail-select-container-sm'))"), 5000, "Select2 Loại chi phí");
    await waitUntil(() => evaluate("document.querySelectorAll('#productRevenueRows [data-detail-row=\"revenue\"] .product-date-input').length === 2"), 5000, "dòng doanh thu động");

    const initialResult = await evaluate(`(() => {
        const costRow = document.querySelector('#productCostRows [data-detail-row="cost"]');
        const costSelect = costRow && costRow.querySelector('.product-detail-select-sm');
        const costSelection = costRow && costRow.querySelector('.product-detail-select-container-sm .select2-selection--single');
        const amountInput = costRow && costRow.querySelector('.js-cost-amount');
        const costDate = costRow && costRow.querySelector('.product-date-input');
        const revenueDates = Array.from(document.querySelectorAll('#productRevenueRows [data-detail-row="revenue"] .product-date-input'));
        const startDate = document.querySelector('#modalProduct .product-date-input');
        return {
            cssLoaded: Array.from(document.styleSheets).some(sheet => (sheet.href || '').indexOf('_ProductModal.css') >= 0),
            productSelect2Count: document.querySelectorAll('#modalProduct .product-service-select + .select2-container').length,
            costSelect2Count: document.querySelectorAll('#modalProduct .product-detail-select + .select2-container').length,
            costSelectExists: Boolean(costSelect),
            costSelectHeight: costSelection ? costSelection.getBoundingClientRect().height : 0,
            amountInputHeight: amountInput ? amountInput.getBoundingClientRect().height : 0,
            datePickerAttached: Boolean(startDate && window.jQuery && jQuery(startDate).data('datepicker')),
            costDatePickerAttached: Boolean(costDate && window.jQuery && jQuery(costDate).data('datepicker')),
            revenueDatePickerCount: revenueDates.filter(input => window.jQuery && jQuery(input).data('datepicker')).length,
            dateIconVisible: Boolean(startDate && getComputedStyle(startDate).backgroundImage !== 'none')
        };
    })()`);

    await evaluate(`(() => {
        const input = document.querySelector('#modalProduct .product-date-input');
        input.click();
    })()`);
    const datePickerVisible = await waitUntil(() => evaluate(`(() => {
        const picker = Array.from(document.querySelectorAll('.datepicker-dropdown')).find(item => {
            const style = getComputedStyle(item);
            const rect = item.getBoundingClientRect();
            return style.display !== 'none' && style.visibility !== 'hidden' && Number(style.opacity) > 0 && rect.width > 0 && rect.height > 0;
        });
        return Boolean(picker);
    })()`), 5000, "popup date picker");

    const datePickerMeasurement = await evaluate(`(() => {
        const picker = Array.from(document.querySelectorAll('.datepicker-dropdown')).find(item => getComputedStyle(item).display !== 'none');
        if (!picker) return null;
        const rect = picker.getBoundingClientRect();
        const style = getComputedStyle(picker);
        return { left: rect.left, top: rect.top, width: rect.width, height: rect.height, visibility: style.visibility, opacity: style.opacity, zIndex: style.zIndex };
    })()`);

    const screenshot = await send("Page.captureScreenshot", { format: "png", captureBeyondViewport: false });
    fs.writeFileSync(screenshotPath, Buffer.from(screenshot.data, "base64"));

    const heightDifference = Math.abs(initialResult.costSelectHeight - initialResult.amountInputHeight);
    const checks = {
        cssLoaded: initialResult.cssLoaded,
        productSelect2InitializedOnce: initialResult.productSelect2Count === 1,
        costSelect2Initialized: !initialResult.costSelectExists || initialResult.costSelect2Count >= 1,
        compactCostSelect: !initialResult.costSelectExists || heightDifference <= 1,
        datePickerAttached: initialResult.datePickerAttached,
        dynamicCostDatePickerAttached: initialResult.costDatePickerAttached,
        dynamicRevenueDatePickersAttached: initialResult.revenueDatePickerCount === 2,
        dateIconVisible: initialResult.dateIconVisible,
        datePickerVisible: datePickerVisible === true
    };
    console.log(JSON.stringify({ checks, measurements: initialResult, datePickerMeasurement, screenshotPath }, null, 2));
    if (Object.values(checks).some(value => value !== true)) process.exitCode = 1;
    browser.kill();
}

main().catch(error => {
    console.error(error.message);
    process.exitCode = 1;
});
