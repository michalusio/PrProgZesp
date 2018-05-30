var webSocket = new WebSocket("ws://localhost:5001/ws");
webSocket.onmessage = function(message) {
    var obj = JSON.parse(message.data);
    switch (obj.type) {
        case "login":
            $("#Users").children().eq(0).children().eq(0).append(`
                    <tr class="user${obj.id}">
                        <td onclick="talkUser(${obj.id})">
                            <image src="data:image/png;base64,${obj.avatar}"/>
                            ${obj.user}
                        </td>
                        <td onclick="friendUser(${obj.id})">
                            <span class="glyphicon glyphicon-plus" aria-hidden="true"></span>
                        </td>
                        <td onclick="blockUser(${obj.id})">
                            <span class="glyphicon glyphicon-ban-circle" aria-hidden="true"></span>
                        </td>
                    </tr>
                `);
            break;
        case "logout":
            $("#Users").children().eq(0)
                .children().eq(0)
                .children(`.user${obj.id}`).eq(0)
                .remove();
            break;
        case "message":
            var conv = $(`#conv${obj.id}`);
            if (conv.length === 0) {
                postGetUsers2(obj.id,obj,function(nicks,obj) {
                    var convers = $("#Conversations").children().eq(0).children().eq(0);
                    convers.append(`<tr id="conv${obj.id}" class="bolder">
                            <td onclick="switchConversation(${obj.id})">
                                <div>
                                    ${nicks.nicknames.join()}
                                </div>
                                <div>
                                </div>
                                <span class="badge">1</span>
                            </td>
                        </tr>`);
                    conv = $(`#conv${obj.id}`);
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
            break;
    }
};