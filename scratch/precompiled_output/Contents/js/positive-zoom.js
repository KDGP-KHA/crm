(function (window) {
    let defineLibrary = () => ({
        init: function (galleryId) {
            let container = document.querySelector(galleryId);

            if (!container) {
                console.error('please add the correct element')
                return;
            }
            //select class small image and zoom image  

            let fristImg = container.querySelector('.small-preview');
            let imgZoom = container.querySelector('.zoom-image');

            //Error message for a programmer who has forgotten the class .zoom-image
            if (!imgZoom) {
                console.error('please add a .zoom-image tag');
                return;
            }

            //Error message for a programmer who has forgotten the class .small-preview

            if (!fristImg) {
                console.error('please add images with .small-preview class to your gallery');
                return;
            }
            //Give the address of the first thumbnail to display in the .zoom-image class

            imgZoom.style.backgroundImage = `url(${fristImg.src})`

            //Clicking on any thumbnail will display that image in the .zoom-image class
            container.addEventListener('click', function (e) {
                let ele = e.target;

                if (ele.classList.contains('small-preview')) {
                    imgZoom.style.backgroundImage = `url(${ele.src})`
                }
            })
            //zoom 
            imgZoom.addEventListener('mouseenter', function () {
                this.style.backgroundSize = '200%';
            })
            //Mouse address in zoom class
            imgZoom.addEventListener('mousemove', function (e) {
                let dimentions = this.getBoundingClientRect()

                let x = e.clientX - dimentions.left
                let y = e.clientY - dimentions.top

                x = Math.round(100 / (dimentions.width / x));
                y = Math.round(100 / (dimentions.height / y));

                this.style.backgroundPosition = `${x}% ${y}%`
            })
            //Mouse out of zoom range
            //imgZoom.addEventListener('mouseleave', function () {
            //    this.style.backgroundSize = '850px';
            //    this.style.backgroundPosition = 'center'
            //})
            imgZoom.addEventListener('mouseleave', function () {
                if (window.innerWidth > 1200) { // Màn hình lớn hơn 768px
                    this.style.backgroundSize = '850px';
                    this.style.backgroundPosition = 'center';
                }
                else if (window.innerWidth <= 1200 && window.innerWidth > 992) {
                    this.style.backgroundSize = '700px';
                    this.style.backgroundPosition = 'center';
                }
                else if (window.innerWidth <= 992 && window.innerWidth > 768) {
                    this.style.backgroundSize = '600px';
                    this.style.backgroundPosition = 'center';
                }
                else if (window.innerWidth <= 768 && window.innerWidth > 576) {
                    this.style.backgroundSize = '400px';
                    this.style.backgroundPosition = 'center';
                }
                else {
                    this.style.backgroundSize = '350px';
                    this.style.backgroundPosition = 'center';
                }
            })
        }
    })


    //To avoid interference between library

    if (typeof (positiveZoom) == 'undefined') {
        window.positiveZoom = defineLibrary();
    } else {
        console.log('library already defined.')
    }
})(window)
