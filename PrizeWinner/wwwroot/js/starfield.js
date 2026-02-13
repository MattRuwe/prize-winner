window.starfield = (function () {
    let canvas, ctx;
    let stars = [];
    let animationId = null;
    let resizeObserver = null;
    let winnerName = null;

    const MAX_Z = 800;
    const MIN_Z = 1;
    const SPEED = 2;
    const FOV = 300;

    function createStar(name, randomZ) {
        return {
            name: name,
            x: (Math.random() - 0.5) * 1600,
            y: (Math.random() - 0.5) * 1000,
            z: randomZ ? Math.random() * MAX_Z : MAX_Z,
            isWinner: false
        };
    }

    function resizeCanvas() {
        if (!canvas) return;
        const dpr = window.devicePixelRatio || 1;
        const rect = canvas.parentElement.getBoundingClientRect();
        canvas.width = rect.width * dpr;
        canvas.height = rect.height * dpr;
        ctx.scale(dpr, dpr);
        canvas.style.width = rect.width + 'px';
        canvas.style.height = rect.height + 'px';
    }

    function render() {
        if (!canvas || !ctx) return;

        const dpr = window.devicePixelRatio || 1;
        const w = canvas.width / dpr;
        const h = canvas.height / dpr;
        const centerX = w / 2;
        const centerY = h / 2;

        ctx.clearRect(0, 0, w, h);

        // Sort back-to-front
        stars.sort((a, b) => b.z - a.z);

        for (let i = 0; i < stars.length; i++) {
            const star = stars[i];

            star.z -= SPEED;
            if (star.z <= MIN_Z) {
                star.z = MAX_Z;
                star.x = (Math.random() - 0.5) * 1600;
                star.y = (Math.random() - 0.5) * 1000;
            }

            const screenX = (star.x / star.z) * FOV + centerX;
            const screenY = (star.y / star.z) * FOV + centerY;

            // Skip if off-screen
            if (screenX < -200 || screenX > w + 200 || screenY < -100 || screenY > h + 100) {
                continue;
            }

            const scale = 1 - star.z / MAX_Z;
            const fontSize = Math.max(12, Math.min(40, 14 + scale * 28));
            const opacity = Math.max(0.15, Math.min(1, scale * 1.3));

            ctx.save();
            ctx.globalAlpha = opacity;
            ctx.font = 'bold ' + fontSize + 'px "Helvetica Neue", Helvetica, Arial, sans-serif';
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';

            if (star.isWinner) {
                ctx.shadowColor = '#d4a017';
                ctx.shadowBlur = 20;
                ctx.fillStyle = '#b8860b';
            } else {
                ctx.shadowColor = 'rgba(27, 110, 194, 0.4)';
                ctx.shadowBlur = 6;
                ctx.fillStyle = '#1b6ec2';
            }

            ctx.fillText(star.name, screenX, screenY);
            ctx.restore();
        }

        animationId = requestAnimationFrame(render);
    }

    return {
        init: function (canvasId) {
            canvas = document.getElementById(canvasId);
            if (!canvas) return;
            ctx = canvas.getContext('2d');

            resizeCanvas();

            resizeObserver = new ResizeObserver(function () {
                resizeCanvas();
            });
            resizeObserver.observe(canvas.parentElement);

            animationId = requestAnimationFrame(render);
        },

        updateNames: function (nameList, winner) {
            winnerName = winner || null;

            // Build a set of current names for quick lookup
            const nameSet = new Set(nameList);
            const existingNames = new Set(stars.map(s => s.name));

            // Remove stars no longer in the list
            stars = stars.filter(s => nameSet.has(s.name));

            // Add new stars
            for (let i = 0; i < nameList.length; i++) {
                if (!existingNames.has(nameList[i])) {
                    stars.push(createStar(nameList[i], true));
                }
            }

            // Update winner status
            for (let i = 0; i < stars.length; i++) {
                stars[i].isWinner = (stars[i].name === winnerName);
            }
        },

        destroy: function () {
            if (animationId) {
                cancelAnimationFrame(animationId);
                animationId = null;
            }
            if (resizeObserver) {
                resizeObserver.disconnect();
                resizeObserver = null;
            }
            canvas = null;
            ctx = null;
            stars = [];
            winnerName = null;
        }
    };
})();
