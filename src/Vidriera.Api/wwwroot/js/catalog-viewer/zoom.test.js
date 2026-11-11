import { test } from "node:test";
import assert from "node:assert/strict";
import { isZoomControl, isIndexJump } from "./zoom.js";

function fakeTarget(matchingSelectors) {
    return {
        closest(selector) {
            const wanted = selector.split(",").map((s) => s.trim());
            return wanted.some((s) => matchingSelectors.includes(s)) ? {} : null;
        },
    };
}

test("a click on the page image is not a control", () => {
    assert.equal(isZoomControl(fakeTarget([])), false);
});

test("buttons, links and inputs are controls", () => {
    assert.equal(isZoomControl(fakeTarget(["button"])), true);
    assert.equal(isZoomControl(fakeTarget(["a"])), true);
    assert.equal(isZoomControl(fakeTarget(["input"])), true);
});

test("anything inside the index or order panels is a control", () => {
    assert.equal(isZoomControl(fakeTarget(["#index-panel"])), true);
    assert.equal(isZoomControl(fakeTarget(["#order-drawer"])), true);
    assert.equal(isZoomControl(fakeTarget(["#page-order-bar"])), true);
});

test("only index items count as index jumps", () => {
    assert.equal(isIndexJump(fakeTarget(["#index-panel .index-item"])), true);
    assert.equal(isIndexJump(fakeTarget(["#index-panel"])), false);
});

test("targets without closest (text nodes, null) are not controls", () => {
    assert.equal(isZoomControl(null), false);
    assert.equal(isZoomControl({}), false);
    assert.equal(isIndexJump(undefined), false);
});
