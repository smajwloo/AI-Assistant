/**
 * An array containing objects with properties id, fileName, and diffs.
 *
 * @typedef {Object} DiffData
 * @property {number} id - The identifier of the object.
 * @property {string} fileName - The name of the file.
 * @property {Array<Diff>} diffs - An array of diff objects.
 */

/**
 * An object representing a diff with optional oldValue and newValue properties.
 *
 * @typedef {Object} Diff
 * @property {number} id - The identifier of the diff.
 * @property {Array<{value: string, selected?: string}>} [oldValue] - The old values.
 * @property {Array<{value: string, selected?: string}>} [newValue] - The new values.
 * @property {Array<{value: string, selected?: string}>} [merged] - The merged values.
 */
