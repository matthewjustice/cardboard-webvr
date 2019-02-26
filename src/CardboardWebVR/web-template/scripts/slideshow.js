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

    // Display the image associated with the current index value.
    // Set the index value before calling this function.
    function displayImageForCurrentIndex() {
        const img = images[index];

        // Set the left and right image values
        const skyleftEl = document.querySelector('#sky-left');
        skyleftEl.setAttribute('src', img.leftImageId);

        const skyrightEl = document.querySelector('#sky-right');
        skyrightEl.setAttribute('src', img.rightImageId);

        // Update the placard text
        const placard = document.querySelector('#placard');
        placard.setAttribute('value', img.caption);

        // Only show the welcome elements on index zero
        if (index === 0) {
            showWelcomeElements(true);
        } else {
            showWelcomeElements(false);
        }
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
        displayImageForCurrentIndex();
    }

    // Shows or hides the cursor
    function showCursor(visible) {
        const element = document.getElementById('cursor');
        element.object3D.visible = visible;
    }

    // Shows or hides the welcome sign and related elements
    function showWelcomeElements(visible) {
        const welcomeElements = document.querySelectorAll('.welcome');
        for (let i = 0, length = welcomeElements.length; i < length; i++) {
            welcomeElements[i].object3D.visible = visible;
        }
    }

    // Alters the look of the navigation arrow
    function illuminateElement(element, illuminate) {
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

            // Initially do not show the cursor
            showCursor(false);
        }
    });

    // By including this component on a nav component
    // a cursor click (gaze / fuse) will cause the slideshow
    // to go forwards, backwards, or home, or to a certain image
    AFRAME.registerComponent('cursor-listener-nav', {
        schema: {
            imageIndex: {type: 'number'}
        },
        init: function() {
            const id = this.el.id;
            const data = this.data;
            this.el.addEventListener('click', function() {
                switch (id) {
                case 'navleft':
                    progressSlideShow(false);
                    break;
                case 'navright':
                    progressSlideShow(true);
                    break;
                case 'navhome':
                    index = 0;
                    displayImageForCurrentIndex();
                    break;
                default:
                    index = data.imageIndex;
                    displayImageForCurrentIndex();
                    showCursor(false);
                    break;
                }
            });

            this.el.addEventListener('mouseenter', function() {
                // Only show the cursor if the element is visible
                if (this.object3D.visible) {
                    showCursor(true);
                    illuminateElement(event.currentTarget, true);
                }
            });

            this.el.addEventListener('mouseleave', function() {
                showCursor(false);
                illuminateElement(event.currentTarget, false);
            });
        }
    });

    // By including this component on an element
    // the cursor will be shown, but there is no
    // click handler. Intended for cursor display only.
    AFRAME.registerComponent('cursor-visible', {
        init: function() {
            this.el.addEventListener('mouseenter', function() {
                // Only show the cursor if the element is visible
                if (this.object3D.visible) {
                    showCursor(true);
                }
            });

            this.el.addEventListener('mouseleave', function() {
                showCursor(false);
            });
        }
    });
}());
