import * as stores from "../../store.js";

export function resetStores() {
    for (const storeName in stores) {
        if (Object.hasOwnProperty.call(stores, storeName)) {
            stores[storeName].set(null);
        }
    }
}
