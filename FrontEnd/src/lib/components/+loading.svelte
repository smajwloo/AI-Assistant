<script>
  import mrBean from "$lib/assets/mr-bean-waiting.gif";

  let jokeSetup;
  let jokeDelivery;
  let showDelivery = false;

  async function getJoke() {
    showDelivery = false;
    try {
      const response = await fetch('https://v2.jokeapi.dev/joke/Programming?type=twopart');
      if (!response.ok) return;

      const data = await response.json();
      jokeSetup = data.setup;
      jokeDelivery = data.delivery;
    } catch (error) {
      console.log(error.message);
    }

    document.querySelector('#get-joke-button').textContent = 'ANOTHER!';
  }

  function displayDelivery() {
    showDelivery = true;
  }
</script>

<div class="flex justify-center mt-5">
  <div class="w-[640px]">
    <img src="{mrBean}" alt="mr-bean-waiting-gif" />

    <div class="break-words mt-2">
      <p>Want to hear a joke in the meantime?</p>
      <button
        class="text-white bg-blue-700 hover:bg-blue-800 focus:ring-4 focus:ring-blue-300 font-medium rounded-lg text-sm px-5 py-2.5 me-2 mb-2 mt-2 focus:outline-none"
        id="get-joke-button"
        on:click="{getJoke}">
        YES!
      </button>


      {#if jokeSetup && jokeDelivery}
        <p>{jokeSetup}</p>
        <button
          class="text-white bg-gray-800 border border-gray-600 focus:outline-none hover:bg-gray-700 focus:ring-gray-700 font-medium rounded-lg text-sm px-5 py-2.5 me-2 mb-2 mt-2"
          on:click={displayDelivery}>
          Tell me!
        </button>

        {#if showDelivery}
          <p>{jokeDelivery}</p>
        {/if}
      {/if}
    </div>
  </div>
</div>