const btn1 = document.getElementById("button")
const btn2 = document.getElementById("button2")

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

async function jsonPost(method, input) {

    const msg = {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(input)
    }

    const response = await fetch(`/json/${method}`, msg) 
    return await response.json() 
}


