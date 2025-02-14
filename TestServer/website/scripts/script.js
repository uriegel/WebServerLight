const btn1 = document.getElementById("button")
const btn2 = document.getElementById("button2")
const btn3 = document.getElementById("button3")
const btn4 = document.getElementById("button4")
const btn5 = document.getElementById("button5")

btn1.onclick = async () => {
    var res = await jsonPost("cmd1", {
        text: "Text",
        id: 123
    })
    console.log("cmd1", res)
}

btn2.onclick = async () => {
    var res = await jsonPost("cmd2", {
        text: "Text",
        id: 123,
        longArray: Array.from(Array(10_000).keys()).map((n, i) => `Item ${i}`)
    })
    console.log("cmd2", res)
}

btn3.onclick = async () => {
    var res = await jsonPost("cmd3", {
        text: "Text",
        id: 123,
        longArray: Array.from(Array(100).keys()).map((n, i) => `Item ${i}`)
    }, true)
    console.log("cmd3", res)
}

btn4.onclick = async () => {
    var res = await jsonPost("cmd4")
    console.log("cmd4", res)
}

btn5.onclick = startWebSockets

async function jsonPost(method, input, base) {

    const msg = {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(input)
    }

    const response = await fetch(base ? `http://127.0.0.23:8080/json/${method}` : `json/${method}`, msg) 
    return await response.json() 
}

function startWebSockets() {
    const ws = new WebSocket("ws://localhost:8080/events")
    ws.onopen = () => console.log("Web socket opened")
    ws.onmessage = evt => console.log("Web socket msg", evt.data, evt)
    ws.onerror = err => console.log("Web socket error", err)
    ws.onclose = () => console.log("Web socket closed")
}
 
