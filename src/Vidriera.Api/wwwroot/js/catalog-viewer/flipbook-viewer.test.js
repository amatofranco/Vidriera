import { test } from "node:test";
import assert from "node:assert/strict";
import { buildPriorityPageList } from "./flipbook-viewer.js";

function sectionEntry(startPage, level = 0) {
    return { isItem: false, level, startPage };
}

function itemEntry(startPage, level = 0) {
    return { isItem: true, level, startPage };
}

test("includes the first min(5, pageCount) pages even with no sectionsData", () => {
    const result = buildPriorityPageList(340, []);
    assert.deepEqual(result, [1, 2, 3, 4, 5]);
});

test("includes all pages when the catalog has fewer than 5 pages", () => {
    const result = buildPriorityPageList(3, []);
    assert.deepEqual(result, [1, 2, 3]);
});

test("never prioritizes item entries, even one per page, in a large catalog", () => {
    const sectionsData = Array.from({ length: 340 }, (_, i) => itemEntry(i + 1));
    const result = buildPriorityPageList(340, sectionsData);
    assert.equal(result.length, 5);
    assert.deepEqual(result, [1, 2, 3, 4, 5]);
});

test("prioritizes up to 15 top-level section covers", () => {
    const sectionsData = Array.from({ length: 20 }, (_, i) => sectionEntry(100 + i, 0));
    const result = buildPriorityPageList(340, sectionsData);
    assert.equal(result.length, 5 + 15);
});

test("prioritizes up to 15 sub-section covers independently of the top-level cap", () => {
    const sectionsData = [
        ...Array.from({ length: 15 }, (_, i) => sectionEntry(100 + i, 0)),
        ...Array.from({ length: 15 }, (_, i) => sectionEntry(200 + i, 1)),
    ];
    const result = buildPriorityPageList(340, sectionsData);
    assert.equal(result.length, 5 + 15 + 15);
});

test("ignores section entries outside the catalog's valid page range", () => {
    const sectionsData = [sectionEntry(0), sectionEntry(341), sectionEntry(10)];
    const result = buildPriorityPageList(340, sectionsData);
    assert.deepEqual(result, [1, 2, 3, 4, 5, 10]);
});
