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
connection.on("ReceiveNowPlaying", function (name, url) {
    // Change the queue if we have to
    // Change the currenttime
    // change the total time
    // change the video src
    // start the video again (muted still)
    // Parse the message with multiple parameters
    console.log("Now Playing:");
    console.log("Name: " + name);
    console.log("URL: " + url);
    // Maybe have to create the toast message after doing other stuff, but we'll see.
    createToast("Shaun " + "Started Playing: " + name);
       
    // Append the toast message element to the body
    document.body.appendChild(toastMessage);

    updateNowPlaying(name, url);
    addToQueue(name, url);

    setPlayers();
    // Remove the toast message after a certain duration (e.g., 5 seconds)
});

connection.on("PausedPlayer", function (message) {
    console.log("PlayerPaused");
    // Pause the video
    // Should pass in the discord username as well.
    createToast("Paused By: " + "Shaun");
});

connection.on("TrackSkipped", function (message) {
    console.log("track skipped");
    createToast("Skipped By: " + "Shaun");
});

connection.on("AddToQueue", function (name, url, position) {
    console.log("AddToQueue");
    // Could even make the url clickable. That would be cool.
    createToast("Shaun " + "Added To Queue:" + name + "\nAt position: " + position);
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});

// If you can't have slider do anything, just have it match the videos time slider's position. There might be a way to send requests to change the time inside the song tho.
// Idk why that wouldn't be built into lavalink and lavalink4net.
// Might need to make a custom player.

// Toast message
function createToast(message) {
    // Create a toast message element
    var toastMessage = document.createElement("div");
    toastMessage.id = "toast-message";
    toastMessage.className = "fixed top-[77px] right-4 bg-blue-500 text-white px-4 py-2 rounded"; // You can adjust the classes as needed
    toastMessage.innerText = message; 

    setTimeout(function () {
        toastMessage.remove();
    }, 5000);
}



// for handling signalr updates
function updateNowPlaying(name, url) {
    const player = document.getElementById('nowPlayingPlayer');
    const video = player.getElementById('nowPlayingVideo');
    const title = player.getElementById('nowPlayingTitle');
    const artist = player.getElementById('nowPlayingArtist');
    const currentTime = player.getElementById('currentTime');
    const duration = player.getElementById('duration');

    const apiUrl = '/get-embed?url=' + encodeURIComponent(url);
    // Do a post here to get the video src
    getEmbed(apiUrl)
        .then(embeddedUrl => {
            video.src = embeddedUrl;
        })
        .catch(error => {
            console.error('Error setting video src:', error);
        });
    title.innerText = name;
    artist.innerText = 'ArtistName';
    currentTime.innerText = '0:00';
    // Have to pass in duration as well.
    duration.innerText = '5:00';
}

function addToQueue(name, url) {
    // Going to add the templated item here. Then make it so that the audio player stuff runs on it again. put that into a function basically then call it.
    // Just make a template with placeholders, and use that everywhere instead, do the replacing here.

    
}

// Requests
function getEmbed(apiUrl) {
    fetch(apiUrl)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.text();
        })
        .then(embeddedUrl => {
            return embeddedUrl;
        })
        .catch(error => {
            console.error('Error:', error);
        });
}

// need to make something that will go thru the queue. That would be cool.
// Because client side and bot side are going to be different. The state just has to match from the javascript.
// Also going to need all the other client's actions to be sent to the other clients.


document.addEventListener("DOMContentLoaded", function () {
    setPlayers();
});

function setPlayers(container) {
    let sounds = null;
    if (container != null) {
        sounds = container.querySelectorAll('[data-itemType="sound"]');
    } else {
        sounds = document.querySelectorAll('[data-itemType="sound"]');
    }
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
}