// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener("DOMContentLoaded", function () {
    var imageModal = document.getElementById("poiImageModal");
    if (!imageModal) {
        return;
    }

    var preview = document.getElementById("poiImagePreview");
    var previewError = document.getElementById("poiImagePreviewError");
    var modalTitle = document.getElementById("poiImageModalLabel");

    if (!preview || !previewError || !modalTitle) {
        return;
    }

    function showPreview(url, title) {
        modalTitle.textContent = title || "Ảnh quán";
        preview.classList.add("d-none");
        previewError.classList.add("d-none");
        preview.src = url || "";
        preview.alt = title || "Ảnh quán";
    }

    preview.addEventListener("load", function () {
        preview.classList.remove("d-none");
        previewError.classList.add("d-none");
    });

    preview.addEventListener("error", function () {
        preview.classList.add("d-none");
        previewError.classList.remove("d-none");
    });

    imageModal.addEventListener("show.bs.modal", function (event) {
        var trigger = event.relatedTarget;
        if (!trigger) {
            return;
        }

        var url = trigger.getAttribute("data-image-url");
        var title = trigger.getAttribute("data-image-title");
        showPreview(url, title);
    });

    imageModal.addEventListener("hidden.bs.modal", function () {
        preview.src = "";
        preview.alt = "";
        preview.classList.add("d-none");
        previewError.classList.add("d-none");
    });
});
