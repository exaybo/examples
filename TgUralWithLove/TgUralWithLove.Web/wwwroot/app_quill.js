//для поддержки множества клилов, каждый для своего контейнера
let quills = new Map();

function initializeQuill(containerSelector) {
    const toolbarOptions = [
        ['bold', 'italic', 'underline', 'strike'],        // toggled buttons
        ['blockquote', 'code-block'],
        ['link'],

        [{ 'header': 1 }, { 'header': 2 }],               // custom button values
        [{ 'list': 'ordered' }, { 'list': 'bullet' }, { 'list': 'check' }],
        [{ 'script': 'sub' }, { 'script': 'super' }],      // superscript/subscript
        [{ 'indent': '-1' }, { 'indent': '+1' }],          // outdent/indent
        [{ 'direction': 'rtl' }],                         // text direction

        [{ 'size': ['small', false, 'large', 'huge'] }],  // custom dropdown
        [{ 'header': [1, 2, 3, 4, 5, 6, false] }],

        [{ 'color': [] }, { 'background': [] }],          // dropdown with defaults from theme
        [{ 'font': [] }],
        [{ 'align': [] }],

        ['clean']                                         // remove formatting button
    ];

    //quill = new Quill('#quill-container', {
    let q = new Quill(containerSelector, {
        modules: {
            toolbar: toolbarOptions,
        },
        placeholder: 'Compose an epic...',
        theme: 'snow', // or 'bubble'
    });
    quills.set(containerSelector, q);
}

function getQuillContent(containerSelector) {
    let q = quills.get(containerSelector)

    return q.root.innerHTML;
}
