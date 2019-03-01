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
        const placard = document.querySelector('#placard-text');
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

    // Adjust the y value of all elements identified with selector
    function adjustHeightForElements(selector, heightDifference) {
        const elements = document.querySelectorAll(selector);
        for (let i = 0, length = elements.length; i < length; i++) {
            const position = elements[i].getAttribute('position');
            position.y += heightDifference;
            elements[i].setAttribute('position', position);
        }
    }

    // Move the user interface elments up or down on mobile based on
    // whether we are in VR or not. This is needed bacause on Chrome
    // mobile, when not in VR, the UI element are very low, some are
    // completely offscreeen (such as the placard), and vertical
    // panning isn't an option on non-VR mobile. Assumption is
    // that this function is first called with inVR=false to adjust
    // downward, and then we adjust upward later if we enter VR.
    function adjustInterfaceHeight(inVR) {
        if (AFRAME.utils.device.isMobile()) {
            if (!inVR) {
                adjustHeightForElements('.user-interface', 0.5);
            } else {
                adjustHeightForElements('.user-interface', -0.5);
            }
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

            // Remove mouse cursor and adjust UI when entering VR
            // This is needed to avoid irregular gaze cursor behavior.
            this.el.sceneEl.addEventListener('enter-vr', function() {
                this.removeAttribute('cursor');
                adjustInterfaceHeight(true);
            });

            // Add mouse cursor and adjust UI when exiting VR
            this.el.sceneEl.addEventListener('exit-vr', function() {
                this.setAttribute('cursor', 'rayOrigin', 'mouse');
                adjustInterfaceHeight(false);
            });

            // Initially do not show the cursor
            showCursor(false);

            // Adjust interface, assume not in VR mode initially
            adjustInterfaceHeight(false);
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
            let ignoreClicks = false;

            this.el.addEventListener('click', function() {
                if (ignoreClicks) {
                    // Igore this click
                    return;
                }

                // On mobile, prevent another click in the short term.
                // This is a workaround for the following issue:
                // https://github.com/aframevr/aframe/issues/3297
                if (AFRAME.utils.device.isMobile()) {
                    ignoreClicks = true;
                    setTimeout(function() {
                        ignoreClicks = false;
                    }, 2000);
                }

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
                    // Used by nav orbs, which are sometimes invisible
                    if (this.object3D.visible) {
                        index = data.imageIndex;
                        displayImageForCurrentIndex();
                        showCursor(false);
                    }
                    break;
                }
            });

            this.el.addEventListener('mouseenter', function(event) {
                // Only show the cursor if the element is visible and the
                // intersecting cursor is the gaze-based cursor.
                const cursorId = event.detail.cursorEl.id;
                if (this.object3D.visible) {
                    if (cursorId == 'cursor') {
                        showCursor(true);
                    }
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
            this.el.addEventListener('mouseenter', function(event) {
                // Only show the cursor if the element is visible and the
                // intersecting cursor is the gaze-based cursor.
                const cursorId = event.detail.cursorEl.id;
                if (this.object3D.visible && cursorId == 'cursor') {
                    showCursor(true);
                }
            });

            this.el.addEventListener('mouseleave', function() {
                showCursor(false);
            });
        }
    });

    // Use this component on a motion controller
    AFRAME.registerComponent('controller-events', {
        init: function() {
            let lastTrackpadX = 0;
            this.el.addEventListener('trackpadup', function(event) {
                if (lastTrackpadX > 0.5) {
                    // Trackpad was last pressed right
                    progressSlideShow(true);
                } else if (lastTrackpadX < -0.5) {
                    // Trackpad was last pressed left
                    progressSlideShow(false);
                } else if (lastTrackpadX > -0.2 && lastTrackpadX < 0.2) {
                    // Trackpad was last pressed in the x center
                    index = 0;
                    displayImageForCurrentIndex();
                }
            });
            this.el.addEventListener('trackpadmoved', function(event) {
                // Record the x position on the trackpad. We get
                // many of these events as the user touches
                // the trackpad, so we don't take any action now, just
                // record the value.
                lastTrackpadX = event.detail.x;
            });
        }
    });
}());
