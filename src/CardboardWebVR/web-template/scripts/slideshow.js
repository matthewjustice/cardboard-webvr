'use strict';

(function() {
    // Track the array of images and the current image index number
    const images = [];
    let index = 0;

    function disposeTextureCache() {
        // Workaround: clear textures from cache
        // Without this, memory usage grows large quickly,
        // sometimes crashing the browser after loading 7 or 8 photos.
        // Reference: https://stackoverflow.com/questions/43940665/how-to-mange-memory-used-by-a-frame
        // Expectation was that the PR below would have fixed this, but maybe it is a difference issue:
        // https://github.com/aframevr/aframe/pull/2686
        Object.keys(AFRAME.scenes[0].systems.material.textureCache).forEach(function(key) {
            AFRAME.scenes[0].systems.material.textureCache[key].then(function(texture) {
                texture.dispose();
            });
        });
    }

    // Move the slide show forwards or backwards.
    function progressSlideShow(forward) {
        // Free up memory if possible
        disposeTextureCache();

        // Determine which image to show next
        if (forward) {
            // Move to the next photo
            index++;
            if (index >= images.length) {
                // at the end, go to the beginning
                index = 0;
            }
        } else {
            // Move to the previous photo
            index--;
            if (index < 0) {
                // at the begining, go to the end
                index = images.length - 1;
            }
        }

        // Display the image
        const img = images[index];

        const skyleftEl = document.querySelector('#sky-left');
        skyleftEl.setAttribute('src', img.leftImageId);

        const skyrightEl = document.querySelector('#sky-right');
        skyrightEl.setAttribute('src', img.rightImageId);

        // Update the placard text
        const placard = document.querySelector('#placard');
        placard.setAttribute('value', img.caption);

        // Only show the welcome text on index zero
        if (index === 0) {
            showWelcomeSign(true);
        } else {
            showWelcomeSign(false);
        }
    }

    // Shows or hides the cursor
    function showCursor(visible) {
        const element = document.getElementById('cursor');
        element.object3D.visible = visible;
    }

    // Shows or hides the welcome sign and related elements
    function showWelcomeSign(visible) {
        const elementIds = ['welcome-sign-border', 'welcome-sign', 'welcome-text',
            'bunny-body', 'bunny-head', 'bunny-eye-right', 'bunny-eye-left',
            'bunny-ear-right', 'bunny-ear-left'];

        for (let i = 0, length = elementIds.length; i < length; i++) {
            const element = document.getElementById(elementIds[i]);
            element.object3D.visible = visible;
        }
    }

    // Alters the look of the navigation arrow
    function illuminateArrow(element, illuminate) {
        if (illuminate) {
            element.setAttribute('color', 'green');
        } else {
            element.setAttribute('color', 'silver');
        }
    }

    // This code runs when the scene is initialized
    AFRAME.registerComponent('sceneinit', {
        init: function() {
            console.log('sceneinit');

            fetch('/images.json')
                .then(function(response) {
                    return response.json();
                })
                .then(function(fetchedImages) {
                    for (let i = 0, length = fetchedImages.length; i < length; i++) {
                        images.push(fetchedImages[i]);
                    }
                })
                .catch(function(error) {
                    console.log('Error while getting images: ' + error.message);
                });

            // Use a long touch as another way to go through the slide show.
            // We need this for mobile scenarios without headsets.
            let timer;
            document.addEventListener('touchstart', function() {
                // When a touch begins, start a time for 1 second.
                // If it isn't canceled before that time, then go to the next image.
                timer = setTimeout(function() {
                    progressSlideShow(true);
                }, 1000);
            }, false);

            document.addEventListener('touchend', function() {
                // If the touch event ends, clear the timer.
                if (timer) {
                    clearTimeout(timer);
                }
            }, false);

            document.addEventListener('keydown', function onKeyDown(event) {
                switch (event.key) {
                case 'ArrowLeft':
                    console.log('left arrow key pressed');
                    progressSlideShow(false);
                    break;
                case 'ArrowRight':
                    console.log('right arrow key pressed');
                    progressSlideShow(true);
                    break;
                }
            });

            // Initiall do not show the cursor
            showCursor(false);

            // When the cursor moves over the navmenu, show the cursor
            document.getElementById('navmenu').addEventListener('mouseenter', function() {
                showCursor(true);
            });

            // When the cursor leaves  the navmenu, hide the cursor
            document.getElementById('navmenu').addEventListener('mouseleave', function() {
                showCursor(false);
            });

            // When the cursor moves over the left nav arrow, show the cursor and
            // illuminate the arrow.
            document.getElementById('navleft').addEventListener('mouseenter', function(event) {
                showCursor(true);
                illuminateArrow(event.currentTarget, true);
            });

            // When the cursor leaves the left nav arrow, hide the cursor and
            // do not illuminuate the arrow.
            document.getElementById('navleft').addEventListener('mouseleave', function(event) {
                showCursor(false);
                illuminateArrow(event.currentTarget, false);
            });

            // When the cursor moves over the right nav arrow, show the cursor and
            // illuminate the arrow.
            document.getElementById('navright').addEventListener('mouseenter', function() {
                showCursor(true);
                illuminateArrow(event.currentTarget, true);
            });

            // When the cursor leaves the right nav arrow, hide the cursor and
            // do not illuminuate the arrow.
            document.getElementById('navright').addEventListener('mouseleave', function() {
                showCursor(false);
                illuminateArrow(event.currentTarget, false);
            });
        }
    });

    // By including this compoennt on the left nav arrow
    // a cursor click (gaze / fuse) will cause the slideshow
    // to go backwards.
    AFRAME.registerComponent('cursor-listener-left', {
        init: function() {
            this.el.addEventListener('click', function(evt) {
                progressSlideShow(false);
            });
        }
    });

    // By including this compoennt on the right nav arrow
    // a cursor click (gaze / fuse) will cause the slideshow
    // to go forwards.
    AFRAME.registerComponent('cursor-listener-right', {
        init: function() {
            this.el.addEventListener('click', function(evt) {
                progressSlideShow(true);
            });
        }
    });
}());
