// could be running in a Firefox extension background page or a Chrome extension ServiceWorker
// Runtime
browser.runtime.onInstalled.addListener(async (e) => {
    console.log('runtime.onInstalled', e);
    // if an app component is found that points to installed.html it will be opened on the onInstalled event
    // load installed page
    //const indexPageUrl = browser.runtime.getURL("app/index.html?_route=popup");
    //browser.tabs.create({
    //    url: indexPageUrl
    //});
});
browser.runtime.onStartup.addListener((e) => {
    console.log(`runtime.onStartup`, e);
});
browser.runtime.onSuspend.addListener((e) => {
    console.log(`runtime.onSuspend`, e);
});
browser.windows.onCreated.addListener(function (window) {
    console.log('windows.onCreated', window);
});
browser.windows.onRemoved.addListener(function (window) {
    console.log('windows.onRemoved', window);
});
// Tabs
browser.tabs.onCreated.addListener(function (e) {
    console.log('tabs.onCreated', e);
});
browser.tabs.onRemoved.addListener(function (e) {
    console.log('tabs.onRemoved', e);
});
browser.tabs.onUpdated.addListener(function (tabId, changeInfo, tab) {
    console.log('tabs.onUpdated', tabId, changeInfo, tab);
    // changeInfo object: https://developer.mozilla.org/en-US/docs/Mozilla/Add-ons/WebExtensions/API/tabs/onUpdated#changeInfo
    if (changeInfo.status === 'complete') {
        // below throws an error if not listening in a content script
        //chrome.tabs.sendMessage(tabId, {
        //    message: 'TabUpdated 111'
        //});
        // chrome-extension://
        // lookign for tabs that are showing an error page due to routing issues and our extension
        console.log('tabs.onUpdated::complete', tabId, tab.url);
    }
});
// WebNavigation
chrome.webNavigation.onBeforeNavigate.addListener((e) => {
    console.log('onBeforeNavigate', e);
});
chrome.webNavigation.onCommitted.addListener((e) => {
    console.log('onCommitted', e);
});
chrome.webNavigation.onDOMContentLoaded.addListener((e) => {
    console.log('onDOMContentLoaded', e);
});
chrome.webNavigation.onCompleted.addListener((e) => {
    console.log('onCompleted', e);
});
chrome.webNavigation.onReferenceFragmentUpdated.addListener((details) => {
    console.log('onReferenceFragmentUpdated', details);
});
chrome.webNavigation.onHistoryStateUpdated.addListener((details) => {
    console.log('onHistoryStateUpdated', details);
});
//chrome.webRequest.onHeadersReceived.addListener((details) => {
//    console.log('onHeadersReceived', details);
//    console.log('headers', details.responseHeaders);
//});
async function shouldPatchCSPCheck(url) {
    return true;
}
// now handled in Blazor
async function patchCSP(request, sender) {
    if (request.cspViolation) {
        var originalPolicy = request.cspViolation.originalPolicy;
        var updatedPolicy = originalPolicy;
        if (originalPolicy.indexOf('wasm-unsafe-eval') === -1) {
            updatedPolicy = originalPolicy.replace('script-src ', "script-src 'wasm-unsafe-eval' ");
        } else {
            // rule already has 'wasm-unsafe-eval'
            // if this happens, there is another problem
            return;
        }
        var url = new URL(request.cspViolation.documentURI);
        // separate paths on the same domain MAY (uncommon) have different csp rules
        // the query string and hash shouldn't have any effect on csp rules
        // check if the extension actualyl wants to patch this csp or not
        // could check user settings.
        var shouldPatchCSP = await shouldPatchCSPCheck(url);
        if (!shouldPatchCSP) {
            // ignore the csp issue
            return;
        }
        var pageUrl = url.origin + url.pathname;
        var pageUrlEscaped = escapeRegExp(pageUrl);
        var cspRule = {
            id: await getFreeSessionRuleId(),
            action: {
                type: 'modifyHeaders',
                responseHeaders: [
                    {
                        header: 'content-security-policy',
                        operation: 'set',
                        value: updatedPolicy,
                    }
                ]

            },
            condition: {
                regexFilter: `^${pageUrlEscaped}(\\?.*)?(#.*)?$`,
                resourceTypes: ["main_frame", "sub_frame", "xmlhttprequest"]
            }
        };
        console.log('Adding rule', cspRule);
        // save rule
        await browser.declarativeNetRequest.updateSessionRules({
            addRules: [cspRule]
        });
        // reload tab so the new rule can take effect
        browser.tabs.reload(sender.tab.id);
    }
}
// https://developer.mozilla.org/en-US/docs/Mozilla/Add-ons/WebExtensions/API/runtime/onMessage
browser.runtime.onMessage.addListener(function (request, sender, sendResponse) {
    console.log('bg.onMessage >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>', request, sender, sendResponse);
    //if (request.cspViolation) {
    //    patchCSP(request, sender);
    //}
});
async function getFreeSessionRuleId() {
    var previousRules = await browser.declarativeNetRequest.getDynamicRules();
    var previousRuleIds = previousRules.map(rule => rule.id);
    if (previousRuleIds.length === browser.declarativeNetRequest.MAX_NUMBER_OF_SESSION_RULES) {
        await browser.declarativeNetRequest.updateSessionRules({ removeRules: previousRuleIds });
        previousRuleIds = [];
    }
    var availId = Math.floor(Math.random() * 1000000) + 1;
    while (previousRuleIds.indexOf(availId) !== -1) availId = Math.floor(Math.random() * 1000000) + 1;
    return availId;
}
// https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Regular_Expressions#escaping
function escapeRegExp(string) {
    return string.replace(/[.*+?^${}()|[\]\\]/g, "\\$&"); // $& means the whole matched string
}
