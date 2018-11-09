var LibraryWebSockets = {
$webSocketInstances: [],

SocketCreate: function(url)
{
	var str = Pointer_stringify(url);
	var socket = {
		socket: new WebSocket(str),
		buffer: new Uint8Array(0),
		error: null,
		messages: []
	}

	socket.socket.binaryType = 'arraybuffer';

	socket.socket.onmessage = function (e) {
		// Todo: handle other data types?
		if (e.data instanceof Blob)
		{
			var reader = new FileReader();
			reader.addEventListener("loadend", function() {
				var array = new Uint8Array(reader.result);
				socket.messages.push(array);
			});
			reader.readAsArrayBuffer(e.data);
		}
		else if (e.data instanceof ArrayBuffer)
		{
			var array = new Uint8Array(e.data);
			socket.messages.push(array);
		}
		else if(typeof e.data === "string") {
			var reader = new FileReader();
			reader.addEventListener("loadend", function() {
				var array = new Uint8Array(reader.result);
				socket.messages.push(array);
			});
			var blob = new Blob([e.data]);
			reader.readAsArrayBuffer(blob);
		}
	};

	socket.socket.onclose = function (e) {
		socket.error = e.code.toString();
		if (e.code != 1000)
		{
			if (e.reason != null && e.reason.length > 0)
				socket.error = e.reason;			
		}
	}
	var instance = webSocketInstances.push(socket) - 1;
	return instance;
},

SocketState: function (socketInstance)
{
	var socket = webSocketInstances[socketInstance];
	return socket.socket.readyState;
},

SocketError: function (socketInstance, ptr, bufsize)
{
 	var socket = webSocketInstances[socketInstance];
 	if (socket.error == null)
 		return 0;
    var str = socket.error.slice(0, Math.max(0, bufsize - 1));
    writeStringToMemory(str, ptr, false);
	return 1;
},

SocketSend: function (socketInstance, ptr, length)
{
	var socket = webSocketInstances[socketInstance];
	socket.socket.send (HEAPU8.buffer.slice(ptr, ptr+length));
},

SocketRecvLength: function(socketInstance)
{
	var socket = webSocketInstances[socketInstance];
	if (socket.messages.length == 0)
		return 0;
	return socket.messages[0].length;
},

SocketRecv: function (socketInstance, ptr, length)
{
	var socket = webSocketInstances[socketInstance];
	if (socket.messages.length == 0)
		return 0;
	if (socket.messages[0].length > length)
		return 0;
	HEAPU8.set(socket.messages[0], ptr);
	socket.messages = socket.messages.slice(1);
},

SocketClose: function (socketInstance)
{
	var socket = webSocketInstances[socketInstance];
	socket.socket.close();
}
};

autoAddDeps(LibraryWebSockets, '$webSocketInstances');
mergeInto(LibraryManager.library, LibraryWebSockets);
