const btn1 = document.getElementById("button")
const btn2 = document.getElementById("button2")
const btn3 = document.getElementById("button3")
const btn4 = document.getElementById("button4")

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

async function jsonPost(method, input, base) {

    const msg = {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(input)
    }

    const response = await fetch(base ? `http://127.0.0.23:8080/json/${method}` : `json/${method}`, msg) 
    return await response.json() 
}


