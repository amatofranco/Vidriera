import { test } from "node:test";
import assert from "node:assert/strict";
import { toHdUrl } from "./hd-pages.js";

test("public bucket page url maps to its -hd.jpg sibling", () => {
    assert.equal(
        toHdUrl("https://pages.example.com/companies/c/catalogs/k/pages/12.jpg"),
        "https://pages.example.com/companies/c/catalogs/k/pages/12-hd.jpg"
    );
});

test("api page url maps to the /hd endpoint", () => {
    assert.equal(
        toHdUrl("https://api.example.com/api/catalogs/k/pages/3"),
        "https://api.example.com/api/catalogs/k/pages/3/hd"
    );
});

test("query string is preserved", () => {
    assert.equal(toHdUrl("https://x/pages/1.jpg?v=2"), "https://x/pages/1-hd.jpg?v=2");
    assert.equal(toHdUrl("https://x/api/catalogs/k/pages/1?v=2"), "https://x/api/catalogs/k/pages/1/hd?v=2");
});

test("unknown urls are left alone", () => {
    assert.equal(toHdUrl(""), null);
    assert.equal(toHdUrl("https://x/logo.png"), null);
    assert.equal(toHdUrl("https://x/pages/1-hd.jpg"), null);
});
