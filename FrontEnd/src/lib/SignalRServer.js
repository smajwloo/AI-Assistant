//@ts-check

import { error } from '@sveltejs/kit';
import { HubConnectionBuilder, HttpTransportType, HubConnection, HubConnectionState } from '@microsoft/signalr';
import {toBase64} from "$lib";
import { PUBLIC_API_URL } from "$env/static/public";

const API_URL = PUBLIC_API_URL || "http://localhost:5000/uploadZip";

/** @type {HubConnection} */
let connection;

/**
 * Start the SignalR connection if it does not already exist.
 * @async
 * @returns {HubConnection} - The current connection to the SignalR server.
 */
export async function getConnection() {
  if (!connection) {
    console.log("No connection found. Creating new connection...")
    connection = new HubConnectionBuilder()
      .withUrl(API_URL, {
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets
      })
      .withAutomaticReconnect([5000, 5000, 5000, 5000, 5000])
      .build();
  }

  if (connection && connection.state === HubConnectionState.Disconnected) {
    await connection.start().catch(err => console.log(err.toString()));
  }

  return connection;
}

/**
 * Upload single chunk of a zip file to the API using the SignalR server.
 * @param {string} connectionId - The id of the connection with the SignalR server
 * @param {string} chunk - The base64 encoded chunk of the zip file.
 * @param {string} fileName - Name of the zip file.
 * @param {string} contentType - The type of the file that is uploaded.
 * @param {number} index - Current index of the total amount of uploaded chunks.
 * @param {number} totalChunks - Total amount of uploaded chunks.
 * @returns {Promise} Returns a string error message if there's no connection or if the connection is not active, otherwise returns nothing.
 * @throws {Error}
 */
export function uploadChunk(connectionId, chunk, fileName, contentType, index, totalChunks) {
  if (!connection) throw error(503, "No SignalR connection found");
  if (connection.state !== HubConnectionState.Connected) throw error(503, "Not connected to SignalR server");

  console.log(`Chunk ${index} send to API`);
  return connection.invoke('UploadChunk', connectionId, fileName, contentType, chunk, index, totalChunks);
}


/**
 * Upload each chunk of the zip file to the API using SignalR.
 * @async
 * @param {string} connectionId - The id of the connection with the SignalR server.
 * @param {Blob[]} fileChunks - The list of chunks.
 * @param {string} fileName - The name of the file before it was sliced into chunks.
 * @param {string} contentType - The content type of the file before it was sliced into chunks.
 */
export async function processFileChunks(connectionId, fileChunks, fileName, contentType) {
  for (let i = 0; i < fileChunks.length; i++) {
    const byteArray = await chunkToByteArray(fileChunks[i]);
    const base64 = toBase64(byteArray);
    uploadChunk(connectionId, base64, fileName, contentType, i, fileChunks.length).catch((err) => {
      console.error(err);
    });
  }
}


/**
 * Convert chunk of the zip file into a ByteArray (Uint8Array).
 * @async
 * @param {Blob} chunk - The chunk to convert.
 */
export async function chunkToByteArray(chunk) {
  const arrayBuffer = await chunk.arrayBuffer();
  return new Uint8Array(arrayBuffer);
}


/**
 * Slices the zip file into mutiple smaller chunks.
 * @param {File} file - Zip file to slice.
 * @returns {Blob[]} Returns a list of chunks.
 */
export function sliceFileIntoChunks(file) {
  const fileChunks = [];
  const chunkSize = 1024 * 1024;
  const totalChunks = Math.ceil(file.size / chunkSize);
  console.log(`Total amount of chunks being created: ${totalChunks}`);

  for (let i = 0; i < totalChunks; i++) {
    const start = i * chunkSize;
    const end = (i + 1) * chunkSize;
    const chunk = file.slice(start, end);
    fileChunks.push(chunk);
  }

  return fileChunks;
}
