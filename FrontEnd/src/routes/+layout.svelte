<script>
  import '../app.css';
  import logo from '$lib/assets/logo.jpeg';
  import { diffStore } from "../store.js";

  let isDiffAvailable = false;
  diffStore.subscribe(value => {
    isDiffAvailable = value && value.length > 0;
  });
</script>

<div class="flex flex-col h-screen">
  <header class="bg-white m-5">
    <nav class="flex items-center">
      <div class="flex">
        <a href="/" class="-m-1.5 p-1.5">
          <img class="h-20 w-auto rounded-full" src="{logo}" alt="logo">
        </a>
      </div>
      <div class="ml-5">
        <a href="/" class="text-sm font-semibold leading-6 text-gray-900">Upload File</a>
      </div>
      <div class="ml-5">
        {#if isDiffAvailable}
          <a href="/differences" class="text-sm font-semibold leading-6 text-gray-900">Differences</a>
        {/if}
      </div>
    </nav>
  </header>

  <div>
    <slot />
  </div>
</div>
