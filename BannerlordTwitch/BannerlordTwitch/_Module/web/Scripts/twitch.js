const twitch = {};

<!-- Twitch User Info -->
$(document).ready(function () {
    const userDataMap = new Map();

    function stringToHslColor(str, s, l) {
        let hash = 0;
        for (let i = 0; i < str.length; i++) {
            hash = str.charCodeAt(i) + ((hash << 5) - hash);
        }
        const h = hash % 360;
        return 'hsl(' + h + ', ' + s + '%, ' + l + '%)';
    }

    function normalizeHexColorToHsl(H, sMax, lMin) {
        // Convert hex to RGB first
        let r = 0, g = 0, b = 0;
        if (H.length === 4) {
            r = "0x" + H[1] + H[1];
            g = "0x" + H[2] + H[2];
            b = "0x" + H[3] + H[3];
        } else if (H.length === 7) {
            r = "0x" + H[1] + H[2];
            g = "0x" + H[3] + H[4];
            b = "0x" + H[5] + H[6];
        }
        // Then to HSL
        r /= 255;
        g /= 255;
        b /= 255;
        const cmin = Math.min(r,g,b);
        const cmax = Math.max(r,g,b);
        const delta = cmax - cmin;
        let h = 0;

        if (delta === 0)
            h = 0;
        else if (cmax === r)
            h = ((g - b) / delta) % 6;
        else if (cmax === g)
            h = (b - r) / delta + 2;
        else
            h = (r - g) / delta + 4;

        h = Math.round(h * 60);

        if (h < 0)
            h += 360;

        let l = (cmax + cmin) / 2;
        let s = delta === 0 ? 0 : delta / (1 - Math.abs(2 * l - 1));
        s = Math.min(sMax, +(s * 100).toFixed(1));
        l = Math.max(lMin, +(l * 100).toFixed(1));

        return "hsl(" + h + "," + s + "%," + l + "%)";
    }
    
    twitch.getUserColor = function(userName) {
        const userData = userDataMap.get(userName.toLowerCase());
        
        if (typeof userData === 'undefined') {
            return stringToHslColor(userName.toLowerCase(), 80, 70)
        }
        return normalizeHexColorToHsl(userData.color, 80, 70);
    }

    function addUser(userName, userData) {
        userDataMap.set(userName.toLowerCase(), userData);
        console.log('BLT Twitch Hub: ' + userName + ' ' + userData.color);
    }

    if (typeof $.connection.twitchHub !== 'undefined') {
        const twitchHub = $.connection.twitchHub;
        twitchHub.client.addUser = addUser;
        $.connection.hub.start().done(function () {
            console.log('BLT Twitch Hub connected');
        }).fail(function () {
            console.log('BLT Twitch Hub could not connect');
        });
    } else {
        addUser("testname", {color: 'green', displayName: 'testName'})
    }
});