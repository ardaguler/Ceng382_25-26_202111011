document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("classForm");
    const tableBody = document.querySelector("#classTable tbody");

    form.addEventListener("submit", function (event) {
        event.preventDefault();

        const className = document.getElementById("className").value;
        const numPeople = document.getElementById("numPeople").value;
        const description = document.getElementById("description").value;

        const row = document.createElement("tr");
        row.innerHTML = `<td>${className}</td><td>${numPeople}</td><td>${description}</td>`;
        
        tableBody.appendChild(row);
        form.reset();
    });

    tableBody.addEventListener("click", function (event) {
        if (event.target.tagName === "TD") {
            const row = event.target.parentElement;
            console.log("Row Clicked: ", row.innerText);
            row.classList.toggle("highlight");
        }
    });

    document.getElementById("classTable").addEventListener("click", function () {
        let entries = [];
        document.querySelectorAll("#classTable tbody tr").forEach(row => {
            entries.push(row.innerText);
        });
        console.log("All Entries:", entries);
    });

    document.querySelectorAll("input, textarea").forEach(input => {
        input.addEventListener("focus", function () {
            this.classList.add("focused");
        });
        input.addEventListener("blur", function () {
            this.classList.remove("focused");
        });
    });

    tableBody.addEventListener("mouseover", function (event) {
        if (event.target.tagName === "TD") {
            event.target.parentElement.style.backgroundColor = "#ddd";
        }
    });

    tableBody.addEventListener("mouseout", function (event) {
        if (event.target.tagName === "TD") {
            event.target.parentElement.style.backgroundColor = "";
        }
    });

    tableBody.addEventListener("dblclick", function (event) {
        if (event.target.tagName === "TD") {
            event.target.parentElement.remove();
        }
    });
});
