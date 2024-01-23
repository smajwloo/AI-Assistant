<script>
    /**@function parentFunc*/
    export let parentFunc;
    /**@type {string}*/
    export let textValue;
    /**@type {HTMLTextAreaElement}*/
    let textArea;
    const minHeight = 15;

    if (textValue && textValue.length === 1) {
        textValue = `${textValue} `;
    }

    $: if (textArea) {
        autoGrow();
    }
    function autoGrow() {
        textArea.style.height = 'auto';
        textArea.style.height = textValue.length < 3 ? `${minHeight}px` : `${textArea.scrollHeight}px`;
    }
</script>

<textarea class="resizeable-textarea"
          bind:this={textArea} {textValue}
          on:input={autoGrow}
          on:click={autoGrow}
          bind:value={textValue}
          on:blur={parentFunc}
/>

<style>
    .resizeable-textarea {
        border: none;
        width: 100%;
        background-color: #ff5b14;
        color: wheat;
        font-family: monospace;
        overflow-y: hidden;
        resize: none;
    }
</style>
