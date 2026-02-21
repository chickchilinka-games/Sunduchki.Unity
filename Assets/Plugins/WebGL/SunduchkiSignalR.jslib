mergeInto(LibraryManager.library, {
    $SunduchkiSignalRHelper: function () {
        if (typeof Module === 'undefined') {
            return null;
        }

        if (typeof Module.SunduchkiSignalR === 'undefined') {
            Module.SunduchkiSignalR = {
                nextId: 1,
                connections: {},
                sendMessage: function (goName, payload) {
                    var instance = typeof unityInstance !== 'undefined' ? unityInstance : Module;
                    if (!instance || typeof instance.SendMessage !== 'function') {
                        console.warn('[SignalR.jslib] Unity instance not found.');
                        return;
                    }

                    try {
                        instance.SendMessage(goName, 'OnSignalRMessage', payload || '');
                    } catch (err) {
                        console.warn('[SignalR.jslib] SendMessage failed:', err);
                    }
                },
                sendEnvelope: function (handle, envelope) {
                    envelope.connectionId = handle.id;
                    Module.SunduchkiSignalR.sendMessage(handle.gameObjectName, JSON.stringify(envelope));
                }
            };
        }

        return Module.SunduchkiSignalR;
    },

    Sunduchki_SignalR_CreateConnection__deps: ['$SunduchkiSignalRHelper'],
    Sunduchki_SignalR_CreateConnection: function (urlPtr, tokenPtr, hostPtr) {
        try {
            if (typeof signalR === 'undefined' || !signalR.HubConnectionBuilder) {
                throw new Error('signalR client is not available. Include @microsoft/signalr script.');
            }

            var helper = SunduchkiSignalRHelper();
            if (!helper) {
                throw new Error('SignalR helper is not available.');
            }
        } catch (err) {
            console.error('[SignalR.jslib] Initialization failed:', err);
            return -1;
        }

        var url = UTF8ToString(urlPtr || 0);
        var token = tokenPtr ? UTF8ToString(tokenPtr) : null;
        var hostName = UTF8ToString(hostPtr || 0);

        try {
            var withUrlOptions = {};
            if (token) {
                withUrlOptions.accessTokenFactory = function () {
                    return token;
                };
            }

            if (signalR.HttpTransportType && signalR.HttpTransportType.WebSockets) {
                withUrlOptions.transport = signalR.HttpTransportType.WebSockets;
                withUrlOptions.skipNegotiation = true;
            }

            var builder = new signalR.HubConnectionBuilder()
                .withUrl(url, withUrlOptions)
                .withAutomaticReconnect();

            var connection = builder.build();
            var id = Module.SunduchkiSignalR.nextId++;

            var handle = {
                id: id,
                connection: connection,
                gameObjectName: hostName,
                handlers: {}
            };

            Module.SunduchkiSignalR.connections[id] = handle;

            connection.onclose(function (error) {
                Module.SunduchkiSignalR.sendEnvelope(handle, {
                    type: 'closed',
                    error: error ? error.toString() : null
                });
                delete Module.SunduchkiSignalR.connections[id];
            });

            connection.onreconnected(function (newConnectionId) {
                Module.SunduchkiSignalR.sendEnvelope(handle, {
                    type: 'reconnected',
                    requestId: newConnectionId || ''
                });
            });

            return id;
        } catch (err) {
            console.error('[SignalR.jslib] Failed to create connection:', err);
            return -1;
        }
    },

    Sunduchki_SignalR_Start__deps: ['$SunduchkiSignalRHelper'],
    Sunduchki_SignalR_Start: function (connectionId) {
        var helper = SunduchkiSignalRHelper();
        var handle = helper ? helper.connections[connectionId] : null;
        if (!handle) {
            return;
        }

        try {
            handle.connection.start()
                .then(function () {
                    helper.sendEnvelope(handle, { type: 'started' });
                })
                .catch(function (err) {
                    helper.sendEnvelope(handle, {
                        type: 'startFailed',
                        error: err ? err.toString() : 'Unknown error'
                    });
                });
        } catch (err) {
            helper.sendEnvelope(handle, {
                type: 'startFailed',
                error: err ? err.toString() : 'Unknown error'
            });
        }
    },

    Sunduchki_SignalR_Stop__deps: ['$SunduchkiSignalRHelper'],
    Sunduchki_SignalR_Stop: function (connectionId) {
        var helper = SunduchkiSignalRHelper();
        var handle = helper ? helper.connections[connectionId] : null;
        if (!handle) {
            return;
        }

        handle.connection.stop()
            .then(function () {
                helper.sendEnvelope(handle, { type: 'stopped' });
            })
            .catch(function (err) {
                helper.sendEnvelope(handle, {
                    type: 'stopped',
                    error: err ? err.toString() : null
                });
            })
            .finally(function () {
                delete helper.connections[connectionId];
            });
    },

    Sunduchki_SignalR_RegisterHandler__deps: ['$SunduchkiSignalRHelper'],
    Sunduchki_SignalR_RegisterHandler: function (connectionId, handlerPtr) {
        var helper = SunduchkiSignalRHelper();
        var handle = helper ? helper.connections[connectionId] : null;
        if (!handle) {
            return;
        }

        var handlerName = UTF8ToString(handlerPtr || 0);
        if (!handlerName) {
            return;
        }

        if (handle.handlers[handlerName]) {
            return;
        }

        var callback = function () {
            var argsJson = JSON.stringify(Array.prototype.slice.call(arguments));
            helper.sendEnvelope(handle, {
                type: 'handler',
                handler: handlerName,
                args: argsJson
            });
        };

        handle.handlers[handlerName] = callback;
        handle.connection.on(handlerName, callback);
    },

    Sunduchki_SignalR_UnregisterHandler__deps: ['$SunduchkiSignalRHelper'],
    Sunduchki_SignalR_UnregisterHandler: function (connectionId, handlerPtr) {
        var helper = SunduchkiSignalRHelper();
        var handle = helper ? helper.connections[connectionId] : null;
        if (!handle) {
            return;
        }

        var handlerName = UTF8ToString(handlerPtr || 0);
        if (!handlerName) {
            return;
        }

        var callback = handle.handlers[handlerName];
        if (!callback) {
            return;
        }

        handle.connection.off(handlerName, callback);
        delete handle.handlers[handlerName];
    },

    Sunduchki_SignalR_Invoke__deps: ['$SunduchkiSignalRHelper'],
    Sunduchki_SignalR_Invoke: function (connectionId, requestIdPtr, methodPtr, argsJsonPtr) {
        var helper = SunduchkiSignalRHelper();
        var handle = helper ? helper.connections[connectionId] : null;
        if (!handle) {
            return;
        }

        var methodName = UTF8ToString(methodPtr || 0);
        var requestId = UTF8ToString(requestIdPtr || 0);
        var argsJson = argsJsonPtr ? UTF8ToString(argsJsonPtr) : null;

        var argsArray = [];
        if (argsJson) {
            try {
                var parsed = JSON.parse(argsJson);
                if (Array.isArray(parsed)) {
                    argsArray = parsed;
                } else if (parsed !== undefined && parsed !== null) {
                    argsArray = [parsed];
                }
            } catch (err) {
                console.warn('[SignalR.jslib] Failed to parse invoke args:', err);
            }
        }

        handle.connection.invoke.apply(handle.connection, [methodName].concat(argsArray))
            .then(function (result) {
                helper.sendEnvelope(handle, {
                    type: 'invokeResult',
                    requestId: requestId,
                    success: true,
                    result: result !== undefined ? JSON.stringify(result) : null
                });
            })
            .catch(function (err) {
                helper.sendEnvelope(handle, {
                    type: 'invokeResult',
                    requestId: requestId,
                    success: false,
                    error: err ? err.toString() : 'Invoke failed'
                });
            });
    }
});
