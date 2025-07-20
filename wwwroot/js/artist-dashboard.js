// ===== ARTIST DASHBOARD FUNCTIONALITY =====

class ArtistDashboard {
    constructor() {
        this.init();
    }

    init() {
        console.log('🎨 Inicializando Dashboard del Artista...');
        
        this.setupAnimations();
        this.loadArtistStats();
        this.setupEventListeners();
        
        console.log('✅ Dashboard del artista inicializado correctamente');
    }

    setupAnimations() {
        // Animación de entrada para las tarjetas
        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.style.opacity = '1';
                    entry.target.style.transform = 'translateY(0)';
                }
            });
        }, observerOptions);

        // Aplicar animaciones a las tarjetas
        document.querySelectorAll('.action-card, .management-card, .stat-card').forEach((card, index) => {
            card.style.opacity = '0';
            card.style.transform = 'translateY(30px)';
            card.style.transition = `opacity 0.6s ease ${index * 0.1}s, transform 0.6s ease ${index * 0.1}s`;
            observer.observe(card);
        });
    }

    setupEventListeners() {
        // Efecto parallax sutil en el hero
        window.addEventListener('scroll', () => {
            const scrolled = window.pageYOffset;
            const hero = document.querySelector('.hero-section');
            if (hero) {
                hero.style.transform = `translateY(${scrolled * 0.3}px)`;
            }
        });
    }

    // Función para cargar estadísticas del artista
    async loadArtistStats() {
        try {
            console.log('🔄 Cargando estadísticas del artista...');
            
            const response = await fetch('/Artists/GetArtistStats');
            if (response.ok) {
                const stats = await response.json();
                
                console.log('📊 Estadísticas recibidas:', stats);
                
                // Actualizar los elementos con animación
                this.updateStatWithAnimation('total-songs', stats.totalSongs);
                this.updateStatWithAnimation('total-albums', stats.totalAlbums);
                this.updateStatWithAnimation('total-likes', stats.totalLikes);
                this.updateStatText('last-upload', stats.lastUpload);
                
                console.log('✅ Estadísticas actualizadas correctamente');
            } else {
                console.error('❌ Error al cargar estadísticas:', response.status);
                this.showErrorStats();
            }
        } catch (error) {
            console.error('❌ Error al cargar estadísticas:', error);
            this.showErrorStats();
        }
    }

    // Función para actualizar estadística con animación
    updateStatWithAnimation(elementId, value) {
        const element = document.getElementById(elementId);
        if (!element) return;
        
        // Animación de contador
        let current = 0;
        const increment = value / 30; // 30 frames de animación
        const timer = setInterval(() => {
            current += increment;
            if (current >= value) {
                current = value;
                clearInterval(timer);
            }
            element.textContent = Math.floor(current).toLocaleString();
        }, 50);
    }

    // Función para actualizar texto simple
    updateStatText(elementId, text) {
        const element = document.getElementById(elementId);
        if (!element) return;
        
        // Efecto de fade
        element.style.opacity = '0.5';
        setTimeout(() => {
            element.textContent = text;
            element.style.opacity = '1';
        }, 200);
    }

    // Función para mostrar error en estadísticas
    showErrorStats() {
        this.updateStatText('total-songs', 'Error');
        this.updateStatText('total-albums', 'Error');
        this.updateStatText('total-likes', 'Error');
        this.updateStatText('last-upload', 'Error');
    }

    // Función para recargar estadísticas (útil después de subir contenido)
    refreshStats() {
        this.loadArtistStats();
    }
}

// Función global para recargar estadísticas
window.refreshArtistStats = function() {
    if (window.artistDashboard) {
        window.artistDashboard.refreshStats();
    }
};

// Inicializar cuando se carga el DOM
document.addEventListener('DOMContentLoaded', () => {
    window.artistDashboard = new ArtistDashboard();
});