// globals
var serverId;
var botBaseUrl;
var username;
function setGlobals(botUrl, server, user) {
    botBaseUrl = botUrl;
    serverId = server;
    username = user;
}

document.addEventListener("DOMContentLoaded", function () {
    setPlayers();
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
    connection.on("ReceiveNowPlaying", function (name, url, username, timestamp, artist) {
        console.log("Now Playing:");
        console.log("Name: " + name);
        console.log("URL: " + url);
        // Maybe have to create the toast message after doing other stuff, but we'll see.
        createToast(username + " Started Playing: " + name);

        updateNowPlaying(name, url, artist);
        addHistoryItem(name, url, timestamp, username);
        //addToQueue(name, url);

        setPlayers();
        // Remove the toast message after a certain duration (e.g., 5 seconds)
    });

    connection.on("ReceivePaused", function (username) {
        console.log("PlayerPaused");
        // Pause the video
        // Should pass in the discord username as well.
        createToast("Paused By: " + username);
        pauseVideo();
    });

    connection.on("ReceiveResume", function (username) {
        console.log("Resumed");
        createToast("Resumed By: " + username);
        resumeVideo();
    });

    connection.on("ReceiveSkipped", function (username) {
        console.log("track skipped");
        createToast("Skipped By: " + username);
        skipSong();
    });

    connection.on("ReceiveStopped", function (username) {
        console.log("track stopped");
        createToast("Stopped By: ", username);
        stopVideoUpdate();
    });
    connection.on("ReceiveQueueUpdated", function (name, url, position, addOrRemove, username, timestamp) {
        console.log("AddToQueue");
        // Could even make the url clickable. That would be cool.
        createToast(username + " " + addOrRemove +"ed To Queue:" + name + "\nAt position: " + position);
        addToQueue(name, url);
        addHistoryItem(name, url, timestamp, username);

        setPlayers();
    });

    connection.on("ReceiveSoundPlaying", function (username, name, emoji) {
        console.log("Sound Playing");
        createToast(username + " Played Sound: " + emoji + " " + name);
    });

    connection.start().catch(function (err) {
        return console.error(err.toString());
    });

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

    document.body.appendChild(toastMessage);

    setTimeout(function () {
        toastMessage.remove();
    }, 5000);
}



// for handling signalr updates
function updateNowPlaying(name, url, artistName) {
    const player = document.getElementById('nowPlayingPlayer');
    const video = player.querySelector('#nowPlayingVideo');
    const title = player.querySelector('#nowPlayingTitle');
    const artist = player.querySelector('#nowPlayingArtist');
    const playBtn = player.querySelector('#playVideo');
    const pauseBtn = player.querySelector('#pauseVideo')
    //const currentTime = player.querySelector('#currentTime');
    //const duration = player.querySelector('#duration');

    const apiUrl = '/get-embed?url=' + encodeURIComponent(url);
    // Do a post here to get the video src
    getEmbed(apiUrl)
        .then(videoId => {
            changeVideo(videoId);
        })
        .catch(error => {
            console.error('Error setting video src:', error);
        });
    title.innerText = name;
    artist.innerText = artistName;

    playBtn.classList.add('hidden');
    pauseBtn.classList.remove('hidden');

    //currentTime.innerText = '0:00';
    //// Have to pass in duration as well.
    //duration.innerText = '5:00';
}

// Add a Song to queue
//function addToQueue(name, url) {
//    console.log('adding to queue');
//    const templateContent = document.getElementById('songTemplate').content.cloneNode(true);
//    template.innerHTML = template.innerHTML.replace('placeholder-songname', name).replace('placeholder-src', url);
//    const queue = document.getElementById('queue');
//    queue.appendChild(template);
//}
function addToQueue(name, url) {
    const noResultsQueue = document.getElementById('noQueueResults');
    const queueContent = document.getElementById('queueContent');
    noResultsQueue.classList.add('hidden');
    queueContent.classList.remove('hidden');

    console.log('adding to queue');
    const templateContent = document.getElementById('songTemplate').content.cloneNode(true);

    // Access and replace content directly within the cloned template
    templateContent.querySelector("#songName").textContent = name;
    templateContent.querySelector("#audioSource").src = url;

    queueContent.appendChild(templateContent);
}
function addHistoryItem(name, url, timestamp, username) {
    const noResultsHistory = document.getElementById('noResultsHistory');
    const historyContent = document.getElementById('historyContent');
    noResultsHistory.classList.add('hidden');
    historyContent.classList.remove('hidden');
    console.log('adding to history');
    const templateContent = document.getElementById('historyItemTemplate').content.cloneNode(true);

    // Assuming spans have IDs, adjust these selectors based on your actual HTML structure
    templateContent.querySelector('#placeholder-name').textContent = name;
    templateContent.querySelector('#placeholder-timestamp').textContent = moment(new Date(timestamp)).fromNow();
    templateContent.querySelector('#songLink').href = url;
    templateContent.querySelector('#placeholder-username').textContent = username;

    historyContent.appendChild(templateContent);
}

// Requests
function getEmbed(apiUrl) {
    // Return the promise chain here
    return fetch(apiUrl)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.text();  // This promise resolves to the text of the response
        })
        .catch(error => {
            console.error('Error:', error);
            throw error;  // Re-throw to ensure the caller knows an error occurred
        });
}
// need to make something that will go thru the queue. That would be cool.
// Because client side and bot side are going to be different. The state just has to match from the javascript.
// Also going to need all the other client's actions to be sent to the other clients.

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

// youtube player
var player; // This will hold the YouTube player object
function onPlayerError(event) {
    console.error('Player Error:', event.data);
}

function onPlayerStateChange(event) {
    if (event.data == YT.PlayerState.PLAYING) {
        setInterval(updateProgressBar, 1000); // Update progress every second while playing
        if (playing == false) {
            playing = true;
            const apiUrl = `/Player/resume?serverId=${encodeURIComponent(serverId)}&username=${encodeURIComponent(username)}`;
            botPost(apiUrl);
        }
    }

    if (event.data == YT.PlayerState.PAUSED) {
        if (playing == true) {
            playing = false;
            const apiUrl = `/Player/pause?serverId=${encodeURIComponent(serverId)}&username=${encodeURIComponent(username)}`;
            botPost(apiUrl);
        }
    }



    // Clear interval when video is paused or ends
    //event.target.addEventListener('onStateChange', function (e) {
    //    if (e.data !== YT.PlayerState.PLAYING) {
    //        clearInterval(interval);
    //    }
    //});
}

function togglePlayPause() {
    var state = player.getPlayerState();
    
    if (state == YT.PlayerState.PLAYING) {
        playing = false;
        const apiUrl = `/Player/pause?serverId=${encodeURIComponent(serverId)}&username=${encodeURIComponent(username)}`;
        botPost(apiUrl);

    } else {
        playing = true;
        const apiUrl = `/Player/resume?serverId=${encodeURIComponent(serverId)}&username=${encodeURIComponent(username)}`;
        botPost(apiUrl);
        
    }
}

function resumeVideo() {
    const playBtn = document.getElementById('playVideo');
    const pauseBtn = document.getElementById('pauseVideo');
    playBtn.classList.add('hidden');
    pauseBtn.classList.remove('hidden');
    player.playVideo();
}

function pauseVideo() {
    const playBtn = document.getElementById('playVideo');
    const pauseBtn = document.getElementById('pauseVideo');
    pauseBtn.classList.add('hidden');
    playBtn.classList.remove('hidden');
    player.pauseVideo();
}

function skipClick() {
    const apiUrl = `/Player/skip?serverId=${encodeURIComponent(serverId)}&username=${encodeURIComponent(username)}`;
    botPost(apiUrl);
}

function skipSong() {
    const queue = document.getElementById('queueContent');
    const queueCount = queue.children.length;
    if (queueCount == 0) {
        player.stopVideo();
    } else {
        const queueItem = queue.querySelector('#queueItemContainer');
        const videoId = queueItem.attributes["data-videoid"];
        player.changeVideo(videoId);
        player.playVideo();
        queueItem.remove();
        if (queueCount == 1) {
            queue.classList.add('hidden');
            noQueueResults.classList.remove('hidden');
        }
    }
}

function stopVideoUpdate() {
    player.stopVideo();
}

function seekClick(event) {
    var seekBar = document.getElementById('customSeekBar');
    var seekBarRect = seekBar.getBoundingClientRect();
    var clickPosition = (event.clientX - seekBarRect.left) / seekBarRect.width;
    var newTime = Math.floor(clickPosition * player.getDuration());

    //const position = Math.floor(player.getCurrentTime());

    const apiUrl = `/Player/seek?serverId=${encodeURIComponent(serverId)}&position=${encodeURIComponent(newTime)}`;
    botPost(apiUrl)
}

function seekVideo(newTime) {
    player.seekTo(newTime, true);
}

function botPost(apiUrl) {
    fetch(botBaseUrl + apiUrl, {
        method: 'POST', 
        headers: {
            'Content-Type': 'application/json' 
        }
    })
    .then(response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return response.json(); 
    })
    .then(data => {
        console.log('Success:', data);
    })
    .catch(error => {
        console.error('Error:', error);
    });
}

function updateProgressBar() {
    var currentTime = player.getCurrentTime();
    var duration = player.getDuration();
    var progressPercent = (currentTime / duration) * 100;
    document.querySelector('[data-type="seekSlider"] div').style.width = progressPercent + "%";
    document.getElementById('nowPlayingCurrentTime').textContent = formatTime(currentTime);
}

function updateDurationDisplay() {
    var duration = player.getDuration();
    document.getElementById('nowPlayingDuration').textContent = formatTime(duration);
}

function formatTime(time) {
    var minutes = Math.floor(time / 60);
    var seconds = Math.floor(time % 60);
    seconds = seconds < 10 ? "0" + seconds : seconds;
    return minutes + ":" + seconds;
}

function changeVideo(videoId) {
    const noResults = document.getElementById('noVideoResults');
    const videoPlayer = document.getElementById('nowPlayingVideo');
    //const noResultsQueue = document.getElementById('noQueueResults');
    //const queueContent = document.getElementById('queueContent');

    noResults.classList.add('hidden');
    videoPlayer.classList.remove('hidden');
    //noResultsQueue.classList.add('hidden');
    //queueContent.classList.remove('hidden');

    player.loadVideoById(videoId);


    //document.getElementById('nowPlayingTitle').textContent = title;
    //document.getElementById('nowPlayingArtist').textContent = artist;
}


// interactions
function toggleMute(el) {
    const muteIcon = el.querySelector('#mute');
    const unmuteIcon = el.querySelector('#unmute');
    if (player.isMuted()) {
        muteIcon.classList.remove('hidden');
        unmuteIcon.classList.add('hidden');
        player.unMute();
    } else {
        muteIcon.classList.add('hidden');
        unmuteIcon.classList.remove('hidden');
        player.mute();
    }
}

//function skipSong(nextVideoId) {
//    changeVideo(nextVideoId);
//}

// Emojioneareas
$(document).ready(function () {
    // initialize emoji selector
    // TODO generalize this
    try {
        $("#EmoteSelector").emojioneArea({
            pickerPosition: "bottom",
            filtersPosition: "bottom",
            tones: false,
            autocomplete: false,
            inline: true,
            darkMode: true,
            events: {
                keyup: function (editor, event) {
                    let content = this.getText();
                    let matches = content.match(/[\ud800-\udbff][\udc00-\udfff]/g);
                    if (matches && matches.length > 0) {
                        this.setText(matches[0]);
                    } else {
                        this.setText('');
                    }
                },
                paste: function (editor, event) {
                    setTimeout(() => {
                        let content = this.getText();
                        let matches = content.match(/[\ud800-\udbff][\udc00-\udfff]/g);
                        if (matches && matches.length > 0) {
                            this.setText(matches[0]);
                        } else {
                            this.setText('');
                        }
                    }, 100);
                }
            }
        });
    } catch (error) {
        console.error("Error initializing EmojiOne Area:", error);
    }
    try {
        $("#EditEmoteSelector").emojioneArea({
            pickerPosition: "bottom",
            filtersPosition: "bottom",
            tones: false,
            autocomplete: false,
            inline: true,
            darkMode: true,
            events: {
                keyup: function (editor, event) {
                    let content = this.getText();
                    let matches = content.match(/[\ud800-\udbff][\udc00-\udfff]/g);
                    if (matches && matches.length > 0) {
                        this.setText(matches[0]);
                    } else {
                        this.setText('');
                    }
                },
                paste: function (editor, event) {
                    setTimeout(() => {
                        let content = this.getText();
                        let matches = content.match(/[\ud800-\udbff][\udc00-\udfff]/g);
                        if (matches && matches.length > 0) {
                            this.setText(matches[0]);
                        } else {
                            this.setText('');
                        }
                    }, 100);
                }
            }
        });
    } catch (error) {
        console.error("Error initializing EmojiOne Area:", error);
    }

    document.getElementById('SaveNewSound').onsubmit = function () {
        const emoteInput = $("#EmoteSelector").data("emojioneArea").getText();
        const isValidEmoji = /^[\ud800-\udbff][\udc00-\udfff]$/.test(emoteInput);

        if (!isValidEmoji) {
            alert('Please select exactly one emoji.');
            return false;
        }

        return true;
    };

    document.getElementById('EditSound').onsubmit = function () {
        const emoteInput = $("#EditEmoteSelector").data("emojioneArea").getText();
        const isValidEmoji = /^[\ud800-\udbff][\udc00-\udfff]$/.test(emoteInput);

        if (!isValidEmoji) {
            alert('Please select exactly one emoji.');
            return false;
        }

        return true;
    };
});

// populate modals onclick
function setModalSoundId(modalId, soundId) {
    fetch(`/Home/GetSoundDetails?id=${soundId}`)
        .then(response => response.json())
        .then(data => {
            console.log(data.name);
            console.log(data.emote);

            var modal = document.getElementById(modalId);
            var hidSoundId = modal.querySelector('#SoundId');

            if (hidSoundId) {
                hidSoundId.value = soundId;
            }

            if (modalId == "editSoundModal") {
                var nameInput = document.getElementById('EditName');
                if (nameInput) {
                    nameInput.value = data.name;
                }

                var emoteInput = $('#EditEmoteSelector');
                if (emoteInput.data("emojioneArea")) {
                    emoteInput.data("emojioneArea").setText(data.emote);
                }
            }
            else if (modalId == "deleteSoundModal") {
                var soundName = document.getElementById('deleteSoundName');
                if (soundName) {
                    soundName.innerText = data.emote + data.name;
                }
            }
        })
        .catch(error => console.error('Error fetching sound details:', error));
}