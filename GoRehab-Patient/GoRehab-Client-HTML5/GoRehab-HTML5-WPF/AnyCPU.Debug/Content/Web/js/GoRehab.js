var JointType = {
    HipCenter: 0,
    Spine: 1,
    ShoulderCenter: 2,
    Head: 3,
    ShoulderLeft: 4,
    ElbowLeft: 5,
    WristLeft: 6,
    HandLeft: 7,
    ShoulderRight: 8,
    ElbowRight: 9,
    WristRight: 10,
    HandRight: 11,
    HipLeft: 12,
    KneeLeft: 13,
    Ankle: 14,
    FootLeft: 15,
    HipRight: 16,
    KneeRight: 17,
    AnkleRight: 18,
    FootRight: 19
}

$(document).ready(function () {
    var streamImageWidth = 640;
    var streamImageHeight = 480;
    var streamImageResolution = streamImageWidth.toString() + "x" + streamImageHeight.toString();

    var isSensorConnected = false;
    var engagedUser = null;
    var cursor = null;
    var userViewerCanvasElement = null;
    var backgroundRemovalCanvasElement = null;
    var canvas = null;
    var context = null;

    // Log errors encountered during sensor configuration
    function configError(statusText, errorData) {
        console.log((errorData != null) ? JSON.stringify(errorData) : statusText);
    }

    // Determine if the specified object has any properties or not
    function isEmptyObject(obj) {
        if (obj == null) {
            return true;
        }

        var numProperties = 0;

        for (var prop in obj) {
            if (obj.hasOwnProperty(prop)) {
                ++numProperties;
            }
        }

        return numProperties <= 0;
    }

    // Show or hide the cursor
    function setCursorVisibility(isVisible) {
        if (cursor == null) {
            return;
        }

        if (isVisible) {
            cursor.show();
        } else {
            cursor.hide();
        }
    }

    // Show or hide a canvas element
    function setCanvasVisibility(canvasElement, isVisible) {
        if (canvasElement == null) {
            return;
        }

        var canvasQuery = $(canvasElement);

        if (isVisible) {
            if (!canvasQuery.hasClass("showing")) {
                // Clear canvas before showing it
                var canvasContext = canvasElement.getContext("2d");
                canvasContext.clearRect(0, 0, streamImageWidth, streamImageHeight);
            }

            canvasQuery.addClass("showing");
        } else {
            canvasQuery.removeClass("showing");
        }
    }

    var showPanelLabel = "Choose<br/>Background";
    var hidePanelLabel = "Hide<br/>Panel";
    function setChoosePanelVisibility(isVisible) {
        var togglePanelElement = document.getElementById("togglePanelButton");
        var spanQuery = $("span", togglePanelElement);

        if (isVisible) {
            // If choose background panel is being shown, hide toggle button
            // border so top of button is aligned with other buttons
            $("#choosePanel").addClass("showing");
            spanQuery.html(hidePanelLabel);
        } else {
            // If choose background panel is being hidden, show toggle button
            // border to provide contrast against picture
            $("#choosePanel").removeClass("showing");
            spanQuery.html(showPanelLabel);
        }
    }

    function isChoosePanelVisible() {
        return $("#choosePanel").hasClass("showing");
    }

    // property and function used to keep track of when the choose background control panel
    // should be hidden after an inactivity period
    var hidePanelTimeoutId = null;
    function resetHidePanelTimeout() {
        // First clear any previous timeout
        if (hidePanelTimeoutId != null) {
            clearTimeout(hidePanelTimeoutId);
            hidePanelTimeoutId = null;
        }

        if (!isSensorConnected || (engagedUser == null)) {
            // if there is no engaged user or no sensor connected, we hide the choose background
            // control panel after 10 seconds
            hidePanelTimeoutId = setTimeout(function () {
                setChoosePanelVisibility(false);
                hidePanelTimeoutId = null;
            }, 10000);
        }
    }

    // Update sensor state and perform UI transitions (showing/hiding appropriate UI elements)
    // related to sensor status or engagement state changes
    var delayedConfigTimeoutId = null;
    function updateUserState(newIsSensorConnected, newEngagedUser, sensorToConfig) {
        var hasEngagedUser = engagedUser != null;
        var newHasEngagedUser = newEngagedUser != null;

        // If there's a pending configuration change when state changes again, cancel previous timeout
        if (delayedConfigTimeoutId != null) {
            clearTimeout(delayedConfigTimeoutId);
            delayedConfigTimeoutId = null;
        }

        if ((isSensorConnected != newIsSensorConnected) || (engagedUser != newEngagedUser)) {
            if (newIsSensorConnected) {

                var immediateConfig = {};
                var delayedConfig = {};
                immediateConfig[Kinect.INTERACTION_STREAM_NAME] = { "enabled": true };
                immediateConfig[Kinect.USERVIEWER_STREAM_NAME] = { "resolution": streamImageResolution };
                immediateConfig[Kinect.USERVIEWER_STREAM_NAME].enabled = true;//QUITAR
                immediateConfig[Kinect.SKELETON_STREAM_NAME] = { "enabled": true };//Activa el skeleto
                immediateConfig[Kinect.BACKGROUNDREMOVAL_STREAM_NAME] = { "resolution": streamImageResolution };
                immediateConfig[Kinect.BACKGROUNDREMOVAL_STREAM_NAME].enabled = true;
                

                setCursorVisibility(newHasEngagedUser);
                setCanvasVisibility(userViewerCanvasElement, true);
                setCanvasVisibility(backgroundRemovalCanvasElement, newHasEngagedUser);

                /*if (newHasEngagedUser) {
                    immediateConfig[Kinect.BACKGROUNDREMOVAL_STREAM_NAME].enabled = true;
                    immediateConfig[Kinect.BACKGROUNDREMOVAL_STREAM_NAME].trackingId = newEngagedUser;

                    delayedConfig[Kinect.USERVIEWER_STREAM_NAME] = { "enabled": false };
                } else {
                    immediateConfig[Kinect.USERVIEWER_STREAM_NAME].enabled = true;

                    if (hasEngagedUser) {
                        delayedConfig[Kinect.BACKGROUNDREMOVAL_STREAM_NAME] = { "enabled": false };
                    }
                }*/

                // Perform immediate configuration
                sensorToConfig.postConfig(immediateConfig, configError);

                // schedule delayed configuration for 2 seconds later
                if (!isEmptyObject(delayedConfig)) {
                    delayedConfigTimeoutId = setTimeout(function () {
                        sensorToConfig.postConfig(delayedConfig, configError);
                        delayedConfigTimeoutId = null;
                    }, 2000);
                }
            } else {
                setCursorVisibility(false);
                setCanvasVisibility(userViewerCanvasElement, false);
                //setCanvasVisibility(backgroundRemovalCanvasElement, false);
            }
        }

        isSensorConnected = newIsSensorConnected;
        engagedUser = newEngagedUser;

        resetHidePanelTimeout();
    }

    // Get the id of the engaged user, if present, or null if there is no engaged user
    function findEngagedUser(userStates) {
        var engagedUserId = null;

        for (var i = 0; i < userStates.length; ++i) {
            var entry = userStates[i];
            if (entry.userState == "engaged") {
                engagedUserId = entry.id;
                break;
            }
        }

        return engagedUserId;
    }

    // Respond to user state change event
    function onUserStatesChanged(newUserStates) {
        var newEngagedUser = findEngagedUser(newUserStates);

        updateUserState(isSensorConnected, newEngagedUser, sensor);
    }

    // Create sensor and UI adapter layers
    var sensor = Kinect.sensor(Kinect.DEFAULT_SENSOR_NAME, function (sensorToConfig, isConnected) {
        if (isConnected) {
            // Determine what is the engagement state upon connection
            sensorToConfig.getConfig(function (data) {
                var engagedUserId = findEngagedUser(data[Kinect.INTERACTION_STREAM_NAME].userStates);

                updateUserState(true, engagedUserId, sensorToConfig);
            });
        } else {
            updateUserState(false, engagedUser, sensorToConfig);
        }
    });
    var uiAdapter = KinectUI.createAdapter(sensor);

    uiAdapter.promoteButtons();
    cursor = uiAdapter.createDefaultCursor();
    userViewerCanvasElement = document.getElementById("userViewerCanvas");
    backgroundRemovalCanvasElement = document.getElementById("backgroundRemovalCanvas");
    uiAdapter.bindStreamToCanvas(Kinect.USERVIEWER_STREAM_NAME, userViewerCanvasElement);
    uiAdapter.bindStreamToCanvas(Kinect.BACKGROUNDREMOVAL_STREAM_NAME, backgroundRemovalCanvasElement);

    sensor.addEventHandler(function (event) {
        switch (event.category) {
            case Kinect.USERSTATE_EVENT_CATEGORY:
                switch (event.eventType) {
                    case Kinect.USERSTATESCHANGED_EVENT_TYPE:
                        onUserStatesChanged(event.userStates);
                        break;
                }
                break;
        }
    });

    sensor.addStreamFrameHandler(function (frame) {

        switch (frame.stream) {
            case Kinect.SKELETON_STREAM_NAME:
                for (var iSkeleton = 0; iSkeleton < frame.skeletons.length; ++iSkeleton) {
                    var skeleton = frame.skeletons[iSkeleton];
                    
                    skeleton.trackingId;
                    skeleton.trackingState;
                    skeleton.position;
                    
                    for (var iJoint = 0; iJoint < skeleton.joints.length; ++iJoint) {
                        var joint = skeleton.joints[iJoint];
                        joint.jointType;
                        joint.trackingState;
                        joint.position;

                        if (JointType.Head == joint.jointType && joint.position.x != 0 && joint.position.y != 0 && joint.position.z != 0) {
                            var x = joint.position.x;
                            var y = joint.position.y;
                            var z = joint.position.z;
                            console.log("Cabeza - X: " + x + ", Y: " + y + ", Z: " + z);
                            var kin = document.getElementById("sampleName");
                            kin.innerHTML = x + "<br>" + y + "<br>" + z + "<br>" /*+Math.atan2(x, z)*/;
                            /*var x = skeleton.boneOrientations[ JointType.ShoulderCenter ].absoluteRotation.matrix.m13;
                            var y = skeleton.boneOrientations[ JointType.ShoulderCenter ].absoluteRotation.matrix.m23;
                            var z = skeleton.boneOrientations[ JointType.ShoulderCenter ].absoluteRotation.matrix.m33;*/
                        }
                    }
                }

                break;
        }
    });

    setChoosePanelVisibility(false);
    $("#togglePanelButton").click(function (event) {
        if (isChoosePanelVisible()) {
            setChoosePanelVisibility(false);
        } else {
            setChoosePanelVisibility(true);
            resetHidePanelTimeout();
        }
    });
    $(".imgButtonContainer .kinect-button").click(function (event) {
        var imgElement = $("img", event.currentTarget)[0];
        document.getElementById("backgroundImg").src = imgElement.src;
        resetHidePanelTimeout();
    });
});