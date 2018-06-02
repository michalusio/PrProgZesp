var webSocket = new WebSocket("ws://localhost:5001/ws");
webSocket.onmessage = function(message) {
    var obj = JSON.parse(message.data);
    switch (obj.type) {
        case "login":
            LoginMessage(obj);
            break;
        case "logout":
            LogoutMessage(obj);
            break;
        case "message":
            TextMessage(obj);
            break;
    }
};

function LoginMessage(obj) {
    $("#Users").children().eq(0).children().eq(0).append(`
        <tr class="user${obj.id}">
            <td onclick="talkUser(${obj.id})">
                <image src="data:image/png;base64,${obj.avatar}"/>
                ${obj.user}
            </td>
`+ (obj.friend ? `<td></td>`:`
            <td onclick="friendUser(${obj.id})">
                <span class="glyphicon glyphicon-plus" aria-hidden="true"></span>
            </td>`
        )+`
            <td onclick="blockUser(${obj.id})">
                <span class="glyphicon glyphicon-ban-circle" aria-hidden="true"></span>
            </td>
        </tr>
    `);
}

function LogoutMessage(obj) {
    $("#Users").children().eq(0)
        .children().eq(0)
        .children(`.user${obj.id}`).eq(0)
        .remove();
}

function TextMessage(obj) {
    var conv = $(`#conv${obj.id}`);
    if (conv.length === 0) {
        postGetUsers2(obj.id,obj,function(nicks, msg) {
            var convers = $("#Conversations").children().eq(0).children().eq(0);
            convers.append(`<tr id="conv${msg.id}" class="bolder">
                    <td onclick="switchConversation(${msg.id})">
                        <div>
                            ${nicks.nicknames.join()}
                        </div>
                        <div>
                        </div>
                        <span class="badge">1</span>
                    </td>
                </tr>`);
            conv = $(`#conv${msg.id}`);
            if ($("#Conversation").hasClass("active") && !$("#Conversation").attr("data-id")) {
                $("#Conversation").data("id", msg.id);
            }
            if ($("#Conversation").data("id") === msg.id && $("#Conversation").hasClass("active")) {
                addMessage(msg);
            } else {
                conv.addClass("bolder");
            }
            var tdChildren = conv.children().eq(0).children();
            tdChildren.eq(1).html(msg.text);
            tdChildren.eq(2).html(parseInt(tdChildren.eq(2).html()) + 1);
        });
    } else {
        if ($("#Conversation").hasClass("active") && !$("#Conversation").attr("data-id")) {
            $("#Conversation").data("id", obj.id);
        }
        if ($("#Conversation").data("id") === obj.id && $("#Conversation").hasClass("active")) {
            addMessage(obj);
        } else {
            conv.addClass("bolder");
        }
        var tdChildren = conv.children().eq(0).children();
        tdChildren.eq(1).html(obj.text);
        tdChildren.eq(2).html(parseInt(tdChildren.eq(2).html()) + 1);
    }
}