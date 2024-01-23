<script>
	import { fail, redirect } from "@sveltejs/kit";
	import { CreateDiffDataStructure, getConnection, processFileChunks, resetStores, sliceFileIntoChunks } from "$lib";
	import { HubConnectionState } from "@microsoft/signalr";
	import { goto } from "$app/navigation";
	import { diffStore, errorMessageStore, progressInformationMessageStore, fileAmountStore } from "../store.js";

	const FILE_SIZE_LIMIT_IN_BYTES = 1000 * 1000 * 1000; // 1GB

	/**
	 * Calculates line numbers for each line in a given diff.
	 *
	 * @param {Array.<{id: number, newValue: undefined | string | Array, oldValue: string | Array}>} diff - The diff to calculate line numbers for.
	 * @returns {Array.<{id: number,
	 * newValue: Array.<{oldLineNumber: number, newLineNumber: number, selected: undefined | string, value: string}>,
	 * oldValue: Array.<{oldLineNumber: number, newLineNumber: number, selected: undefined | string, value: string}>}>} - An array of line objects with line numbers, content, and added/removed flags.
	 */
	const calculateLineNumbers = (diff) => {
		let oldLineNumber = 0;
		let newLineNumber = 0;
		const diffCopy = diff.map(chunk => ({ ...chunk })); // Copy for debugging purposes
		return diffCopy.map((chunk) => {
			if (chunk.oldValue) {
				chunk.oldValue = chunk.oldValue.trimEnd('\n').split('\n').map(line => {
					oldLineNumber++;
					if (!chunk.newValue) newLineNumber++;
					return {
						oldLineNumber: oldLineNumber,
						newLineNumber: newLineNumber,
						value: line,
						selected: undefined
					}
				});
			}
			if (chunk.newValue) {
				chunk.newValue = chunk.newValue.trimEnd('\n').split('\n').map(line => {
					newLineNumber++;
					return {
						oldLineNumber: oldLineNumber,
						newLineNumber: newLineNumber,
						value: line,
						selected: undefined
					};
				});
			}
			return chunk;
		});
	};

	async function removeSignalRCallbacks() {
		const connection = await getConnection();

		connection.off('ReceiveProgressInformation');
		connection.off('ReceiveError');
		connection.off('ReceiveLlmResponse');
		connection.off('ReceiveTotalFiles');
	}

	async function registerSignalRCallbacks() {
		const connection = await getConnection();

		connection.on('ReceiveTotalFiles', (_, totalFiles) => {
			console.log(`Total amount of files send to LLM: ${totalFiles}`)
			fileAmountStore.set(totalFiles);
		});

		connection.on('ReceiveProgressInformation', (_, message) => {
			console.log(`Success: ${message}`);
			progressInformationMessageStore.set(message);
		});

		connection.on('ReceiveError', (_, message) => {
			console.log(`ServerError: ${message}`);
			errorMessageStore.set(message);
		});

		connection.on('ReceiveLlmResponse', (_, fileName, contentType, fileContent, oldFileContent) => {
			let diffDataStructure = CreateDiffDataStructure(oldFileContent, fileContent, { ignoreWhitespace: false });
			let diffWithLineNumbers = calculateLineNumbers(diffDataStructure);

			diffWithLineNumbers = removeFileWithoutChanges(diffWithLineNumbers);

			diffStore.update((value) => {
				const diff = {
					id: value ? value.length : 0,
					fileName: fileName,
					diffs: diffWithLineNumbers
				};

				if (value) return [...value, diff];
				return [diff];
			});

			progressInformationMessageStore.set(null);
			errorMessageStore.set(null);
		});
	}

	function removeFileWithoutChanges(differences) {
		let hasNewValue = differences.some(diff => diff.newValue && diff.newValue.length > 0);
		if (!hasNewValue) {
			fileAmountStore.update(value => value - 1);
			return differences.remove(differences.find(diff => !diff.newValue || diff.newValue.length === 0));
		}
		return differences;
	}

	async function submitForm(event) {
		event.preventDefault();
		await removeSignalRCallbacks();
		resetStores();
		const formData = new FormData(event.target);
		const file = formData.get('file');

		if (!(file instanceof File)) return;
		if (file.size <= 0) return fail(400, { error: "No file received or file is empty." });
		if (file.size > FILE_SIZE_LIMIT_IN_BYTES) return fail(413, { error: `File size of file "${file.name}" exceeded the limit of ${FILE_SIZE_LIMIT_IN_BYTES / 1000 / 1000} MB.` });

		const connection = await getConnection();
		await registerSignalRCallbacks();

		if (connection.state === HubConnectionState.Connected) {
			const connectionId = await connection.invoke("GetConnectionId");
			const fileChunks = sliceFileIntoChunks(file);
			await processFileChunks(connectionId, fileChunks, file.name, file.type);
			await goto('/differences');
		}
	}

</script>

<div class="flex flex-grow justify-center items-center">
	<div>
		<h1 class="mb-4 text-4xl font-extrabold leading-none tracking-tight text-gray-900">ZIP-file upload</h1>

		<div class="w-fit mt-5">
			<form on:submit={submitForm} enctype="multipart/form-data">
				<div class="flex items-center p-4 mb-4 text-sm text-blue-800 border border-blue-300 rounded-lg bg-blue-50 w-fit max-w-3xl" role="alert">
					<svg class="flex-shrink-0 inline w-4 h-4 me-3" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 20 20">
						<path d="M10 .5a9.5 9.5 0 1 0 9.5 9.5A9.51 9.51 0 0 0 10 .5ZM9.5 4a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3ZM12 15H8a1 1 0 0 1 0-2h1v-3H8a1 1 0 0 1 0-2h2a1 1 0 0 1 1 1v4h1a1 1 0 0 1 0 2Z"/>
					</svg>
					<div>
						<span class="font-medium">Info!</span> Currently, only TypeScript files within the ZIP file are being processed by the AI.
					</div>
				</div>
				<input class="block w-full text-sm text-gray-900 border border-gray-300 rounded-lg cursor-pointer bg-gray-50 focus:outline-none"
							 aria-describedby="file_input_help"
							 id="file"
							 name="file"
							 type="file"
							 accept=".zip"
							 required>
				<p class="mt-1 text-sm text-gray-500" id="file">Only .zip allowed</p>

				<button class="text-white bg-blue-700 hover:bg-blue-800 focus:ring-4 focus:ring-blue-300 font-medium rounded-lg text-sm px-5 py-2.5 me-2 mb-2 mt-5 focus:outline-none"
								type="submit"
								on:click={() => redirect(300, '/differences')}>
					Upload
				</button>
			</form>
		</div>
	</div>
</div>