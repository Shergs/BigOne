// globals
var serverId;
var botBaseUrl;

function setGlobals(botUrl, server) {
    botBaseUrl = botUrl;
    serverId = server;
}

const connection = new signalR.HubConnectionBuilder()
    .withUrl(botBaseUrl + "/playerinfo-hub?serverId=" + serverId)
    .configureLogging(signalR.LogLevel.Information)
    .build();

// Joining a group
//connection.invoke("AddToGroup", serverId)
//    .catch(function (err) {
//        return console.error(err.toString());
//    });

// Leaving a group
//connection.invoke("RemoveFromGroup", "GroupName")
//    .catch(function (err) {
//        return console.error(err.toString());
//    });

// Listening for messages from the group
connection.on("ReceiveNowPlaying", function (message) {
    console.log("New Message: " + message);
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});