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
    setYTAudioPlayers();
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(botBaseUrl + "/playerinfo-hub?serverId=" + serverId)
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // See if this needs to be imlpemented or if it's doing a full page refresh when switching to the other servers.
    // Prob won't have to actually implement this, but maybe.
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

    connection.on("ReceiveMoveUpInQueue", function (username, position) {
        console.log("Move up in queue");
        createToast(username + " Moved song up in queue at postion: " + position)
        updateMoveUpInQueue(position);
    });

    connection.on("ReceiveMoveDownInQueue", function (username, position) {
        console.log("Move up in queue");
        createToast(username + " Moved song down in queue at postion: " + position)
        updateMoveDownInQueue(position);
    });

    connection.on("ReceiveDeleteFromQueue", function (username, position) {
        console.log("Delete from queue");
        createToast(username + " Deleted song from queue at position: " + position);
        updateDeleteFromQueue(position);
    });

    connection.on("ReceiveSeekVideo", function (username, position) {
        console.log("Video seek");
        createToast(username + " Seeked to seconds: " + position);
        seekVideo(position);
    });

    connection.start().catch(function (err) {
        return console.error(err.toString());
    });

});

// Toast message
function createToast(message, error) {
    console.log('creating toast');
    let backgroundColor = "bg-blue-500";
    if (error != null) {
        backgroundColor = "bg-red-500";
    }

    var toastMessage = document.createElement("div");
    toastMessage.id = "toast-message";
    toastMessage.className = `fixed top-[85px] right-4 ${backgroundColor} text-white px-4 py-2 rounded opacity-100 transition-opacity z-50`;
    toastMessage.innerHTML = `<span>${message}</span>
                              <div class="w-full bg-gray-400 rounded h-1 mt-2 overflow-hidden">
                                  <div class="bg-white h-1 rounded transition-all duration-5000 ease-linear" style="width: 100%;"></div>
                              </div>`;

    document.body.appendChild(toastMessage);

    setTimeout(() => {
        const progressBar = toastMessage.querySelector('div.bg-white');
        if (progressBar) {
            progressBar.style.width = '0%';
        }
    }, 100);

    setTimeout(() => {
        toastMessage.style.opacity = '0';
        setTimeout(() => {
            toastMessage.remove();
        }, 500);
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
}

function addToQueue(name, url) {
    const noResultsQueue = document.getElementById('noQueueResults');
    const queueContent = document.getElementById('queueContent');
    noResultsQueue.classList.add('hidden');
    queueContent.classList.remove('hidden');

    console.log('adding to queue');
    const templateContent = document.getElementById('songTemplate').content.cloneNode(true);
    const queueCount = queueContent.children.length; 

    templateContent.querySelector("#songName").textContent = name;
    //templateContent.querySelector("#audioSource").src = url;
    templateContent.querySelector("#queueItemContainer").attributes["data-queueposition"] = queueCount.toString();

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
            // For the song preview (download) implementation
            //if (audio.paused) {
            //    if (audio.src === "placeholder-src" || !audio.src) {
            //        const ytUrl = sound.getAttribute('data-yt-url'); 
            //        fetch(`/Home/DownloadYtMP3?url=${encodeURIComponent(ytUrl)}`)
            //            .then(response => response.json())
            //            .then(data => {
            //                audio.src = data.audioUrl;
            //                audio.load();
            //                audio.play();
            //                pauseBtn.classList.remove('hidden');
            //                playBtn.classList.add('hidden');
            //            })
            //            .catch(error => {
            //                console.error('Error loading the audio:', error);
            //                // Optionally show an error message to the user
            //            });
            //    } else {
            //        audio.play();
            //        pauseBtn.classList.remove('hidden');
            //        playBtn.classList.add('hidden');
            //    }
            //} else {
            //    audio.pause();
            //    playBtn.classList.remove('hidden');
            //    pauseBtn.classList.add('hidden');
            //}
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

function setYTAudioPlayers() {
    const ytAudioPlayerContainers = document.querySelectorAll('[data-type="youtube-player"]');
    ytAudioPlayerContainers.forEach((container) => {
        const videoId = container.getAttribute('data-videoid');
        new YT.Player(container.id, {
            height: '0',
            width: '0',
            videoId: videoId, 
            playerVars: {
                'autoplay': 0,
                'controls': 0
            },
            events: {
                'onReady': (event) => songPlayerReady(event, container),
                'onStateChange': songPlayerStateChanged,
            }
        });
        container.player = player;
    });
}

function songPlayerReady(event, container) {
    const controls = document.querySelector('[data-uniqueId="' + container.id + '"][data-type="controls"]');
    const playPauseBtn = controls.querySelector('[data-type="playPauseBtn"]');
    const playIcon = playPauseBtn.querySelector('[data-type="play"]');
    const pauseIcon = playPauseBtn.querySelector('[data-type="pause"]');
    const seekSlider = controls.querySelector('[data-type="seekSlider"]');
    const currentTimeDisplay = controls.querySelector('[data-type="currentTime"]');
    const durationDisplay = controls.querySelector('[data-type="duration"]');

    // Set maximum value for the seekSlider based on video duration
    durationDisplay.textContent = formatTime(event.target.getDuration());
    seekSlider.max = event.target.getDuration();

    playPauseBtn.addEventListener('click', () => {
        if (event.target.getPlayerState() === YT.PlayerState.PLAYING) {
            event.target.pauseVideo();
            playIcon.classList.remove('hidden');
            pauseIcon.classList.add('hidden');
        } else {
            event.target.playVideo();
            pauseIcon.classList.remove('hidden');
            playIcon.classList.add('hidden');
        }
    });

    event.target.addEventListener('onStateChange', function (e) {
        if (e.data === YT.PlayerState.PLAYING) {
            const updateTime = () => {
                const currentTime = event.target.getCurrentTime();
                seekSlider.value = currentTime;
                currentTimeDisplay.textContent = formatTime(currentTime);
                if (!event.target.paused) {
                    requestAnimationFrame(updateTime);
                }
            };
            requestAnimationFrame(updateTime);
        }
    });

    seekSlider.addEventListener('input', () => {
        event.target.seekTo(seekSlider.value);
    });
}

function formatTime(time) {
    const minutes = Math.floor(time / 60);
    let seconds = Math.floor(time % 60);
    seconds = seconds < 10 ? '0' + seconds : seconds;
    return minutes + ":" + seconds;
    return minutes + ":" + seconds;
}

function songPlayerStateChanged() {
    
}

// youtube player
var player; // This will hold the YouTube player object
function onPlayerError(event) {
    console.error('Player Error:', event.data);
}

function onPlayerStateChange(event) {
    if (event.data == YT.PlayerState.PLAYING && documentHidden == false) {
        setInterval(updateProgressBar, 1000); // Update progress every second while playing
        if (initialPlay == false) {
            initialPlay = true;
            return;
        }
        if (playing == false) {
            playing = true;
            const apiUrl = `/Player/resume?serverId=${encodeURIComponent(serverId)}&username=${encodeURIComponent(username)}`;
            botPost(apiUrl);
        }
    }

    if (event.data == YT.PlayerState.PAUSED && documentHidden == false) {
        // if the tab isn't visible, then make it not do this post, and hide the video player, and require
        // that the next thing that they do is 'Sync with server' with a button where the video player is supposed
        // to be. 
        if (playing == true) {
            playing = false;
            const apiUrl = `/Player/pause?serverId=${encodeURIComponent(serverId)}&username=${encodeURIComponent(username)}`;
            botPost(apiUrl);
        }
    }

    const queueContent = document.getElementById('queueContent');
    queueCount = queueContent.children.length;
    if (event.data == YT.PlayerState.ENDED) {
        if (queueCount == 0) {
            initialPlay = false;
        } else {
            const queuedVideoId = queueContent.children[0].getAttribute('data-videoid');
            changeVideo(queuedVideoId);
        }
        updateDeleteFromQueue(0);
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
        changeVideo(videoId);
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
    const apiUrl = `/Player/seek?serverId=${encodeURIComponent(serverId)}&seconds=${encodeURIComponent(newTime)}&username=${encodeURIComponent(username)}`;
    botPost(apiUrl);   
}

function seekVideo(newTime) {
    player.seekTo(newTime, true);
    player.playVideo();
}

function moveUpInQueue(position) {
    const apiUrl = `/Player/moveupinqueue?serverId=${encodeURIComponent(serverId)}&index=${encodeURIComponent(position)}&username=${encodeURIComponent(username)}`;
    botPost(apiUrl);
}

function moveDownInQueue(position) {
    const apiUrl = `/Player/movedowninqueue?serverId=${encodeURIComponent(serverId)}&index=${encodeURIComponent(position)}&username=${encodeURIComponent(username)}`;
    botPost(apiUrl);
}

function updateMoveUpInQueue(position) {
    const queueContent = document.getElementById('queueContent');
    const queueCount = queueContent.children.length;

    if (queueCount > 1 && position > 1) {  
        const current = document.querySelector(`[data-queueposition="${position}"]`);
        const prev = document.querySelector(`[data-queueposition="${position - 1}"]`);

        if (current && prev) {  
            queueContent.insertBefore(current, prev);
            current.setAttribute('data-queueposition', position - 1);
            prev.setAttribute('data-queueposition', position);
        }
    }
}

function updateMoveDownInQueue(position) {
    const queueContent = document.getElementById('queueContent');
    const queueCount = queueContent.children.length;

    if (queueCount > 1 && position < queueCount) { 
        const current = document.querySelector(`[data-queueposition="${position}"]`);
        const next = document.querySelector(`[data-queueposition="${parseInt(position) + 1}"]`);

        if (current && next) { 
            queueContent.insertBefore(next, current);
            current.setAttribute('data-queueposition', parseInt(position) + 1);
            next.setAttribute('data-queueposition', position);
        }
    }
}

function deleteFromQueue(position) {
    const apiUrl = `/Player/deletefromqueue?serverId=${encodeURIComponent(serverId)}&position=${encodeURIComponent(position)}&username=${encodeURIComponent(username)}`;
    botPost(apiUrl);
}

function updateDeleteFromQueue(position) {
    const queueContent = document.getElementById('queueContent');
    const queueItemContainer = document.querySelector('[data-queueposition="' + position + '"]');
    if (!queueItemContainer) {
        console.error("No element found with position:", position);
        return; // Exit if no element is found
    }
    queueItemContainer.remove();

    // Update positions: start from the deleted position to the end of the queue
    for (let i = position - 1; i < queueContent.children.length; i++) {
        queueContent.children[i].setAttribute('data-queueposition', i + 1);
    }
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
    const currentTime = player.getCurrentTime();
    if (lastKnownTime != -1 && Math.abs(lastKnownTime - currentTime) > 5 && !refreshing) {
        const apiUrl = `/Player/seek?serverId=${encodeURIComponent(serverId)}&seconds=${encodeURIComponent(Math.floor(currentTime))}&username=${encodeURIComponent(username)}`;
        botPost(apiUrl); 
    }
    refreshing = false;

    const duration = player.getDuration();
    const progressPercent = (currentTime / duration) * 100;
    document.querySelector('[data-type="seekSlider"] div').style.width = `${progressPercent}%`;
    document.getElementById('nowPlayingCurrentTime').textContent = formatTime(currentTime);

    lastKnownTime = currentTime;
}

//function formatTime(time) {
//    const minutes = Math.floor(time / 60);
//    const seconds = Math.floor(time % 60);
//    return `${minutes}:${seconds < 10 ? '0' : ''}${seconds}`;
//}

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


// Generalized dropdown initialize
const dropdowns = document.querySelectorAll('[data-type="dropdown-container"]');
function toggleDropdownList(el) {
    el.classList.toggle('hidden');
}
dropdowns.forEach((dropdownContainer) => {
    const dropdownMenu = dropdownContainer.querySelector('[data-type="dropdown-menu"]');
    const searchInput = dropdownContainer.querySelector('[data-type="search-input"]');
    const dropdownButton = dropdownContainer.querySelector('[data-type="dropdown-button"]');

    dropdownButton.addEventListener('click', () => {
        toggleDropdownList(dropdownMenu);
    });

    searchInput.addEventListener('input', () => {
        const searchTerm = searchInput.value.toLowerCase();
        const items = dropdownMenu.querySelectorAll('a');

        items.forEach((item) => {
            const text = item.textContent.toLowerCase();
            if (text.includes(searchTerm)) {
                item.style.display = 'block';
            } else {
                item.style.display = 'none';
            }
        });
    });

    //dropdownButton.addEventListener('blur', () => {
    //    setTimeout(() => {
    //        if (document.activeElement != searchInput && !Array.from(document.querySelectorAll("[data-dropdown-element='server']")).includes(document.activeElement)
    //            && !Array.from(document.querySelectorAll('[data-type="selection"]')).includes(document.activeElement)) {
    //            toggleDropdownList(dropdownMenu);
    //        }
    //    }, 0);
    //});
    //searchInput.addEventListener('blur', () => {
    //    setTimeout(() => {
    //        if (document.activeElement != dropdownButton && !Array.from(document.querySelectorAll("[data-dropdown-element='server']")).includes(document.activeElement)
    //            && !Array.from(document.querySelectorAll('[data-type="selection"]')).includes(document.activeElement)) {
    //            toggleDropdown(dropdownMenu);
    //        }
    //    }, 0);
    //});
});

var documentHidden = false;
var refreshing = false;
document.addEventListener('visibilitychange', function () {
    if (document.hidden) {
        documentHidden = true;
        console.log('Tab is not active');
    } else {
        documentHidden = false;
        console.log('Tab is active');
        getPlayerPosition(serverId).then(position => {
            refreshing = true;
            player.seekTo(position, true);
        }).catch(error => {
            console.error('Failed to fetch player position:', error);
        });
    }
});

async function getPlayerPosition(serverId) {
    try {
        const response = await fetch(botBaseUrl + `/Player/getplayerposition?serverId=${serverId}`);
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        const position = await response.json();
        return position;
    } catch (error) {
        console.error('Error fetching player position:', error);
        throw error;
    }
}

function sendPlaySong() {
    const container = document.getElementById('playSongToServer');
    const dropdown = container.querySelector('[data-type="dropdown-container"]');
    const dropdownSelection = dropdown.querySelector('[data-type="selected"]').value.trim();
    const query = document.getElementById('newSongQuery').value;

    if (query == '' || dropdownSelection == '') {
        createToast('Could not play song. Information was entered incorrectly.', true);
        return;
    }

    const apiUrl = `/Player/playsong?serverId=${encodeURIComponent(serverId)}&username=${encodeURIComponent(username)}&queryString=${encodeURIComponent(query)}&voiceChannelId=${encodeURIComponent(dropdownSelection)}`;
    botPost(apiUrl);
}

function sendPlaySound(name) {
    const container = document.getElementById('SoundboardCard');
    const dropdown = container.querySelector('[data-type="dropdown-container"]');
    const dropdownSelection = dropdown.querySelector('[data-type="selected"]').value.trim();

    if (dropdownSelection == '') {
        createToast('Must have a Voice Channel selected', true);
        return;
    }

    const apiUrl = `/Player/playsound?serverId=${encodeURIComponent(serverId)}&username=${encodeURIComponent(username)}&soundName=${encodeURIComponent(name)}&voiceChannelId=${encodeURIComponent(dropdownSelection)}`;
    botPost(apiUrl);
}

// Initialize general dropdown selections
document.addEventListener('DOMContentLoaded', function () {
    const dropdownContainers = document.querySelectorAll('[data-type="dropdown-container"]');
    dropdownContainers.forEach(container => {
        container.addEventListener('click', function (event) {
            if (event.target.matches('[data-type="selection"]')) {
                console.log('in dropdown item click');
                event.preventDefault();
                const item = event.target;
                const itemVal = item.getAttribute('data-value');
                const selected = container.querySelector('[data-type="selected"]');
                const dropdownButton = container.querySelector('[data-type="dropdown-button"]');
                selected.value = itemVal;
                dropdownButton.textContent = item.textContent;
            }
        });
    });
});

function openSongToServer() {
    document.getElementById('playSongToServer').classList.toggle('hidden');
    document.getElementById('playSongPlus').classList.toggle('hidden');
    document.getElementById('playSongMinus').classList.toggle('hidden');
}

function toggleQueueHistory() {
    const button = document.getElementById('toggleQueueHistory');
    const queue = document.getElementById('queue');
    if (queue.classList.contains('hidden')) {
        button.textContent = 'See History';
    }
    else {
        button.textContent = 'See Queue';
    }
    document.getElementById('queue').classList.toggle('hidden');
    document.getElementById('history').classList.toggle('hidden');
}