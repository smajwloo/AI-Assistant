<script>
  import AutoGrowingTextArea from "$lib/components/AutoGrowingTextArea.svelte";
  import Popup from '$lib/components/+popup.svelte';
  import { v4 as uuidv4 } from "uuid";
  import { diffStore } from "../../store.js";
  import JSZip from "jszip";

  export let diffItem;
  export let index;

  /** @type {Array<DiffData>}*/
  let mergedStruct;

  diffStore.subscribe((value) => {
    if (value) {
      mergedStruct = value;
      mergedStruct.forEach(diffItem => {
        diffItem.diffs.forEach(diff => {
          if (diff.newValue)
            diff.merged = [];
        });
      });
    }
  });

  function handleClick(diffId, diffItemId, index, old) {
    let diff = diffItem.diffs[diffId];
    if (!(diff.oldValue && diff.newValue)) {
      console.log('returning. No new AND old found');
      return;
    }

    const lineObject = old === true ? diffItem.diffs[diffId].oldValue[index] :
      diffItem.diffs[diffId].newValue[index];

    changeLineColor(lineObject, old, diffId, diffItemId, index);
    moveToAndFromMerged(lineObject, diffId, diffItemId);
  }

  function moveToAndFromMerged(lineObject, diffId, diffItemId) {
    const diffs = mergedStruct[diffItemId].diffs;
    const merged = diffs[diffId].merged;

    let contains;
    if (merged) contains = merged.some(item => item.id === lineObject.id);

    calculateLineNumber(diffs, diffId, lineObject, contains);

    if (!contains) {
      lineObject.id = uuidv4();
      diffs[diffId].merged = [...merged, JSON.parse(JSON.stringify(lineObject))];
    } else {
      diffs[diffId].merged = merged.filter(item => item.id !== lineObject.id);
    }

    diffs[diffId].merged.sort((a, b) => a.newLineNumber - b.newLineNumber);
    mergedStruct = [...mergedStruct];
  }

  function calculateLineNumber(diffs, diffId, lineObject, contains) {
    if (diffId >= diffs.length) return;

    for (let i = diffId; i < diffs.length; i++) {
      diffs[i].oldValue.forEach(value => contains ? value.oldLineNumber-- : value.oldLineNumber++);
    }

    lineObject.lineNumber = contains ? lineObject.newLineNumber - 1 : lineObject.newLineNumber + 1;
  }

  const changeLineColor = (lineObject, old, diffId, diffItemId, index) => {
    if (old) {
      diffItem.diffs[diffId].oldValue[index].selected =
        lineObject.selected === "old" ? undefined : "old";
    } else if (!old) {
      diffItem.diffs[diffId].newValue[index].selected =
        lineObject.selected === "new" ? undefined : "new";
    }
  }

  function getFileName(path) {
    const parts = path.split('/');
    return parts[parts.length - 1];
  }

  function submit(shouldCheckError) {
    let showError = false;
    const download = {}
    mergedStruct.forEach(mergedStructItem => {
      let dataString = "";

      mergedStructItem.diffs.forEach(diff => {
        if (diff.merged && diff.merged.length > 0) {
          // use merged values
          let mergedValues = "";
          diff.merged.forEach(data => mergedValues = mergedValues === "" ? `${data.value}` :`${mergedValues}\n${data.value}`);
          dataString = dataString === "" ? `${mergedValues}` : `${dataString}\n${mergedValues}`;
        } else if (shouldCheckError && diff.oldValue && diff.newValue) {
          // if both oldValue and newValue exist, user forgot to select an option
          showError = true;
        } else {
          // just take oldValue
          let mergedValues = ""
          diff.oldValue.forEach(data => mergedValues = mergedValues === "" ? `${data.value}` :`${mergedValues}\n${data.value}`);
          dataString = dataString === "" ? `${mergedValues}` : `${dataString}\n${mergedValues}`;
        }
      })
      download[mergedStructItem.fileName] = dataString;
    });

    if (showError) togglePopup();
    else createZip(download);

    console.log(download);
  }

  function createZip(dataToZip) {
    const zip = new JSZip();
    for (const [fileName, content] of Object.entries(dataToZip)) {
      const blob = createBlob(content);
      zip.file(fileName, blob);
    }

    zip.generateAsync({ type: "blob" }).then((zipBlob) => {
      const zipUrl = URL.createObjectURL(zipBlob);
      const link = document.createElement("a");
      link.href = zipUrl;
      link.download = "bestanden.zip";
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(zipUrl);
    });
  }

  function createBlob(textContent) {
    return new Blob([textContent], { type: "text/plain" });
  }

  /**
   * Handles text edits on an input element.
   *
   * @param {number} diffId - The ID of the current diff.
   * @param {number} diffItemId - The ID of the current diff item.
   * @param {number} index - The index of the merged code in the diff.
   * @param {InputEvent} event - The input event from the contenteditable element.
   */
  function handleTextBlur(diffId, diffItemId, index, event) {
    mergedStruct[diffItemId].diffs[diffId].merged[index].value = event.target.value;
    mergedStruct = [...mergedStruct];
  }

  function togglePopup() {
    const popup = document.querySelector('#popup-modal');

    if (!popup.style.visibility || popup.style.visibility === 'hidden') {
      popup.style.visibility = 'visible';
      popup.style.backgroundColor = 'rgba(0, 0, 0, 0.5)';
      document.addEventListener('keydown', handleKeyDown);
    } else {
      popup.style.visibility = 'hidden';
      popup.style.backgroundColor = 'rgba(0, 0, 0, 0)';
      document.removeEventListener('keydown', handleKeyDown);
    }
  }

  function handleKeyDown(event) {
    if (event.key === 'Escape') togglePopup();
  }
</script>

<div class="fixed top-4 left-1/2 transform -translate-x-1/2">
  <button class="bg-blue-700 hover:bg-blue-800 text-white font-bold py-2 px-4 rounded-full shadow-lg"
          data-modal-target="popup-modal"
          data-modal-toggle="popup-modal"
          type="button"
          on:click={() => submit(true)}>
    Submit
  </button>
</div>

<Popup on:submit={() => {submit(false); togglePopup()}} on:toggle={togglePopup} />

{#if diffItem && mergedStruct}
  <div id="code-container" class="column-container">
    <div class="code maxxed">
      <h2 class="mb-4 text-4xl font-extrabold leading-none tracking-tight text-gray-900">Original:</h2>
      <h3 class="mb-4 text-3xl font-extrabold leading-none tracking-tight text-gray-900 break-all">{getFileName(diffItem.fileName)}</h3>

      {#each diffItem.diffs as diff}
        {#each diff.oldValue as oldCode, oldIndex}
          <div class="code-diff wrap {diff.oldValue && diff.newValue ? 'removed' : 'unchanged'}"
               tabindex="0"
               class:selected-old={oldCode.selected === "old"}
               on:click={() => handleClick(diff.id, diffItem.id, oldIndex, true)}
               on:keydown={() => handleClick(diff.id, diffItem.id, oldIndex, true)}
               role="button">

            <span>{oldCode.oldLineNumber}</span>
            <pre>{oldCode.value}</pre>
          </div>
        {/each}
      {/each}
    </div>

    <div class="code maxxed">
      <h2 class="mb-4 text-4xl font-extrabold leading-none tracking-tight text-gray-900">Merged:</h2>
      <h3 class="mb-4 text-3xl font-extrabold leading-none tracking-tight text-gray-900">{getFileName(mergedStruct[index].fileName)}</h3>

      {#each mergedStruct[index].diffs as diff}
        {#if diff.merged}
          {#if diff.merged.length > 0}
            {#each diff.merged as mergedCode, mergedIndex}
              <div class="code-diff merged-item removable-merge-item"
                   tabindex="0"
                   role="button">
                <span>{mergedCode.lineNumber}</span>
                <AutoGrowingTextArea textValue="{mergedCode.value}" parentFunc="{(event) => handleTextBlur(diff.id, diffItem.id, mergedIndex, event)}" />
              </div>
            {/each}
          {:else}
            <div class="code-diff merged-item"
                 tabindex="0"
                 role="button">
              <pre> </pre>
            </div>
          {/if}
        {:else}
          {#each diff.oldValue as oldCode}
            <div class="code-diff unchanged wrap" role="button">
              <span>{oldCode.oldLineNumber}</span>
              <pre>{oldCode.value}</pre>
            </div>
          {/each}
        {/if}
      {/each}
    </div>

    <div class="code maxxed">
      <h2 class="mb-4 text-4xl font-extrabold leading-none tracking-tight text-gray-900">Generated:</h2>
      <h3 class="mb-4 text-3xl font-extrabold leading-none tracking-tight text-gray-900 text-pretty">{getFileName(diffItem.fileName)}</h3>

      {#each diffItem.diffs as diff}
        {#if diff.newValue}
          {#each diff.newValue as newCode, newIndex}
            <div class="code-diff wrap {diff.oldValue && diff.newValue ? 'added' : 'unchanged'}"
                 tabindex="0"
                 class:selected-new={newCode.selected === "new"}
                 on:click={() => handleClick(diff.id, diffItem.id, newIndex, false)}
                 on:keydown={() => handleClick(diff.id, diffItem.id, newIndex, false)}
                 role="button">
              <span>{newCode.newLineNumber}</span>
              <pre>{newCode.value}</pre>
            </div>
          {/each}
        {:else}
          {#each diff.oldValue as oldCode}
            <div class="code-diff unchanged wrap" role="button">
              <span>{oldCode.newLineNumber}</span>
              <pre>{oldCode.value}</pre>

            </div>
          {/each}
        {/if}
      {/each}
    </div>
  </div>
{/if}

<style>
    .wrap {
        word-break: break-word;
        max-width: 100%;
    }

    .column-container {
        column-count: 2;
        column-gap: 30px;
        display: flex;
        justify-content: space-between;
    }

    .removable-merge-item {
        display: flex;
        flex-direction: row;
    }

    .code {
        flex: 1;
        display: flex;
        height: 100%;
        width: 99%;
        flex-direction: column;
        margin: 0 2px 0 5px;
    }

    .maxxed {
        max-width: 70rem;
    }

    .code-diff {
        margin: 0 0 3px 0;
        display: flex;
    }

    .code-diff span {
        margin: 0;
        padding-right: 10px;
        word-break: keep-all;
    }

    .selected-new {
        background-color: #2eef00 !important;
        font-weight: bold;
    }

    .selected-old {
        background-color: #ff1414 !important;
        font-weight: bold;
    }

    .merged-item {
        background-color: #ff5b14 !important;
        font-weight: bold;
    }

    .added {
        background-color: rgba(117, 243, 155, 0.49);
        color: #24292e;
        text-decoration: none;
    }

    .unchanged {
        color: #24292e;
    }

    .removed {
        background-color: rgba(241, 113, 130, 0.65);
        color: #24292e;
        text-decoration: line-through;
    }

    .added:hover, .removed:hover {
        cursor: pointer;
    }

    pre {
        white-space: pre-wrap;
        margin: 0;
    }
</style>