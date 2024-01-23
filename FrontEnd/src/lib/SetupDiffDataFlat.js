import { diffLines } from "diff";

/**
 * Creëert een diff data object.
 * @param {number} id - Het ID van het item.
 * @param {string} oldString - De oude versie van de string.
 * @param {string} newString - De nieuwe versie van de string.
 * @returns {object} - Het diff data object.
 */
function createDiffItem(id, oldString, newString) {
    return {
        id: id,
        oldValue: oldString,
        newValue: newString,
    };
}

/**
 * Creëert een lijst van diff data objecten op basis van de verschillen tussen twee bestandsinhouden.
 * @param {string} oldFileContent - De oude bestandsinhoud.
 * @param {string} newFileContent - De nieuwe bestandsinhoud.
 * @param {object} options - Opties voor het diff proces.
 * @returns {object[]} - De lijst van diff data objecten.
 */
export function CreateDiffDataStructure(oldFileContent, newFileContent, options) {
    const res = diffLines(oldFileContent, newFileContent, options);
    return createIntelligentDiffs(res);
}

/**
 * Creëert intelligente diffs van de gegeven resultaten.
 * @param {object[]} res - De resultaten van de diff.
 * @returns {object[]}
 */
function createIntelligentDiffs(res) {
    let finalObjectList = [];
    let previousObjectItem = null;

    res.forEach((currentItem, index) => {
        if (currentItem.removed) {
            const item = createDiffItem(
                finalObjectList.length,
                currentItem.value,
                ""
            );
            finalObjectList.push(item);
            previousObjectItem = item;
        } else if (currentItem.added) {
            if (previousObjectItem === null) {
                console.error("Error: previousItem is null");
                return;
            }
            previousObjectItem.newValue = currentItem.value;
            previousObjectItem = null;
        } else {
            // no change
            finalObjectList.push(createDiffItem(
                finalObjectList.length,
                currentItem.value,
                undefined
            ));
        }
    });

    return finalObjectList;
}
