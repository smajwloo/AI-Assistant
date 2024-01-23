import { diffLines } from "diff";

export class DiffItem {
    /**
     * @param {number} id - The item's id.
     * @param {string} oldString - The old version.
     * @param {string} newString - The new version.
     */
    constructor(id, oldString, newString) {
        this.id = id;
        this.old = oldString;
        this.new = newString;
        this.previousObject = null;
        this.nextObject = null;
    }
}

/**
 * @param {string} oldFileContent - The old version.
 * @param {string} newFileContent - The new version.
 * @param {object} options - The options.
 * @returns {DiffItem[]} - The list of items.
 */
export function CreateDiffDataStructure(oldFileContent, newFileContent, options) {
    const res = diffLines(oldFileContent, newFileContent, options);
    const objects = createIntelligentDiffs(res);
    return linkObjects(objects);
}

/**
 * @param {DiffItem[]} objects - The list of items.
 * @returns {DiffItem[]} - The list of items.
 */
function linkObjects(objects) {
    for (let i = 0; i < objects.length; i++) {
        const previousItem = objects[i - 1];
        const currentItem = objects[i];
        const nextItem = objects[i + 1];

        currentItem.previousObject = previousItem;
        currentItem.nextObject = nextItem;
    }
    return objects;
}

/**
 * @param {object[]} res - The list of objects.
 * @returns {DiffItem[]}
 */
function createIntelligentDiffs(res) {
    /** @type {DiffItem[]} */
    let finalObjectList = []
    /** @type {DiffItem} */
    let previousObjectItem = null;

    for (let i = 0; i < res.length; i++) {
        const currentItem = res[i];

        if (currentItem.removed) {
            const item = new DiffItem(
                finalObjectList.length,
                currentItem.value,
                ""
            )
            finalObjectList.push(item);
            previousObjectItem = item;
        } else if (currentItem.added) {
            if (previousObjectItem === null) {
                console.error("Error: previousItem is null");
            }
            previousObjectItem.new = currentItem.value;
            previousObjectItem = null;
        } else {
            // no change
            let noChangeObj = new DiffItem(
                finalObjectList.length,
                currentItem.value,
                undefined
            )
            finalObjectList.push(noChangeObj);
        }
    }

    return finalObjectList;
}
