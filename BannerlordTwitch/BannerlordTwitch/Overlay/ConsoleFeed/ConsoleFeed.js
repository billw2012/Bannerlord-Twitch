$(function () {
    const bltConsole = new Vue({
        el: '#bltconsole-container',
        data: {
            items: [],
            internalId: -1,
        }
    });
    
    function stringToHslColor(str, s, l) {
        let hash = 0;
        for (let i = 0; i < str.length; i++) {
            hash = str.charCodeAt(i) + ((hash << 5) - hash);
        }
        const h = hash % 360;
        return 'hsl('+h+', '+s+'%, '+l+'%)';
    }
    
    function addMessage(message) {
        const goldRegex = /(\d*⦷)/g;
        const userNameRegex = /(@[a-zA-Z0-9]*)/g;
        const splitMessage = message.message
            .split(goldRegex)
            .map(s => s.split(userNameRegex))
            .reduce((a, b) => a.concat(b))
            .map(s => {
                if(s.match(goldRegex))
                    return "<span class='gold-text-style'>" + s + "</span>";
                else if(s.match(userNameRegex)) {
                    const nameColor = stringToHslColor(s, 80, 75);
                    return "<span class='username-text-style' style='color: " + nameColor
                        + "'>" + s + "</span><span class='default-text-style'></span>";
                }
                return "<span class='default-text-style'>" + s + "</span>";
            });
        const processedMessage = {
            id: message.id,
            message: splitMessage.join(''),
            style: message.style
        };
        bltConsole.items.push(processedMessage);
        if(bltConsole.items.length > 100)
        {
            bltConsole.items.shift();
        }
        //console.log(processedMessage);
    }

    if(typeof $.connection.consoleFeedHub !== 'undefined') {
        $.connection.hub.url = '$url_root$/signalr';
        $.connection.hub.error(function (error) {
            console.log('Overlay error: ' + error);
            // bltConsole.items.push({ id: bltConsole.internalId--, message: 'Overlay error: ' + error, style: 'fail' });
        });
        $.connection.hub.starting(function () {
            console.log('Overlay starting');
            bltConsole.items.push({ id: bltConsole.internalId--, message: 'Overlay starting...', style: 'internal' });
        });
        $.connection.hub.connectionSlow(function () {
            console.log('Overlay connectionSlow');
            bltConsole.items.push({ id: bltConsole.internalId--, message: 'Overlay connection slow', style: 'internal' });
        });
        $.connection.hub.reconnecting(function () {
            console.log('Overlay reconnecting');
            bltConsole.items.push({ id: bltConsole.internalId--, message: 'Overlay reconnecting...', style: 'internal' });
        });
        $.connection.hub.reconnected(function () {
            console.log('Overlay reconnected');
            bltConsole.items.push({ id: bltConsole.internalId--, message: 'Overlay reconnected', style: 'internal' });
        });
        $.connection.hub.disconnected(function () {
            console.log('Overlay disconnected');
            bltConsole.items.push({ id: bltConsole.internalId--, message: 'Overlay disconnected', style: 'internal' });
        });
        const consoleFeedHub = $.connection.consoleFeedHub;
        
        consoleFeedHub.client.addMessage = addMessage;
        $.connection.hub.start().done(function () {
            console.log('BLT Console Hub connected');
        }).fail(function () {
            bltConsole.items.push({
                id: bltConsole.internalId--, message: 'BLT Console Hub started could not connect',
                style: 'fail'
            });
            console.log('BLT Console Hub started could not connect');
        });
    } else {
        addMessage({ id: 0, message: "TESTING MODE", style: "system" });
        addMessage({ id: 1, message: "Version x.y.z", style: "system" });
        addMessage({ id: 2, message: "@testName: some message!", style: "response" });
    }
});