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
    // Change the queue if we have to
    // Change the currenttime
    // change the total time
    // change the video src
    // start the video again (muted still)

    console.log("New Message: " + message);
});

connection.on("PausedPlayer", function (message) {
    console.log("PlayerPaused");
});

connection.on("TrackSkipped", function (message) {
    console.log("track skipped");
});

connection.on("AddToQueue", function (message) {
    console.log("AddToQueue");
});

// toast message for all of these events

connection.start().catch(function (err) {
    return console.error(err.toString());
});


// need to make something that will go thru the queue. That would be cool.
// Because client side and bot side are going to be different. The state just has to match from the javascript.
// Also going to need all the other client's actions to be sent to the other clients.