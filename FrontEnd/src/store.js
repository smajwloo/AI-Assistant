import { persisted } from "svelte-local-storage-store";

export const progressInformationMessageStore = persisted('progressInformationMessageStore', null, { storage: "session" });
export const errorMessageStore = persisted('errorMessageStore', null, { storage: "session" });
export const oldCodeStore = persisted('oldCodeStore', null, { storage: "session" });
export const newCodeStore = persisted('newCodeStore', null, { storage: "session" });
export const diffStore = persisted('diffStore', null, { storage: "session" });
export const fileAmountStore = persisted('fileAmountStore', null, { storage: "session" });
