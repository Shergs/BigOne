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


document.addEventListener("DOMContentLoaded", function () {
    let sounds = document.querySelectorAll('[data-itemType="sound"]');
    sounds.forEach((sound) => {
        let playPauseBtn = sound.querySelector('[data-type="playPauseBtn"]');
        let audio = sound.querySelector('audio');
        let seekSlider = sound.querySelector('[data-type="seekSlider"]');
        let currentTimeDisplay = sound.querySelector('[data-type="currentTime"]');
        let durationDisplay = sound.querySelector('[data-type="duration"]')

        audio.addEventListener("loadedmetadata", function () {
            seekSlider.max = audio.duration;
            durationDisplay.textContent = formatTime(audio.duration);
        });
        audio.addEventListener("timeupdate", function () {
            seekSlider.value = audio.currentTime;
            currentTimeDisplay.textContent = formatTime(audio.currentTime);
        });
        playPauseBtn.addEventListener("click", function () {
            const pauseBtn = playPauseBtn.querySelector('[data-type="pause"]');
            const playBtn = playPauseBtn.querySelector('[data-type="play"]');
            if (audio.paused) {
                audio.play();
                pauseBtn.classList.remove('hidden');
                playBtn.classList.add('hidden');
            } else {
                audio.pause();
                playBtn.classList.remove('hidden');
                pauseBtn.classList.add('hidden');
            }
        });
        seekSlider.addEventListener("input", function () {
            audio.currentTime = seekSlider.value;
        });
    });

    function formatTime(seconds) {
        let minutes = Math.floor(seconds / 60);
        seconds = Math.floor(seconds % 60);
        seconds = seconds < 10 ? "0" + seconds : seconds;
        return minutes + ":" + seconds;
    }
});