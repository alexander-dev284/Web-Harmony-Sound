// HarmonySound - Reproductor compartido
// Usar window.harmonyConfig para configurar URLs y opciones en cada vista

(function () {
    'use strict';

    class HarmonyPlayer {
        constructor() {
            const cfg = (window.harmonyConfig || {});
            this.urls = cfg.urls || {};
            this.userId = cfg.userId || '';

            this.audio = null;
            this.currentTrack = null;
            this.isPlaying = false;
            this.volume = 0.5;
            this.playlist = [];
            this.originalPlaylist = [];
            this.currentTrackIndex = 0;
            this.isLooping = false;
            this.isShuffling = false;
            this.isPremiumUser = false;
            this.likedContentIds = new Set();
            this.isProgressDragging = false;
            this.isVolumeDragging = false;
            this.isPlayingAd = false;
            this.adEndedHandler = null;
            this.originalTrackToPlay = null;
            this.adNotification = null;
            this.adCountdownInterval = null;

            // Selectores - detecta automáticamente el tipo de fila en la vista
            this.ROW_SEL = '.track-row, .song-item';
            this.PLAY_SEL = '.play-track-btn, .play-in-player-btn';
            this.LIKE_SEL = '.like-btn, .track-like-btn';

            this.init();
        }

        // ── Inicialización ─────────────────────────────────────────────────

        async init() {
            this.audio = document.getElementById('hidden-audio') || this._createAudioEl();
            this.playerEl = document.getElementById('custom-player');

            this._getRefs();
            this._buildPlaylist();
            this._bindEvents();
            this.setVolume(this.volume);

            try {
                await this._checkPremium();
                await this._loadLikes();
            } catch (e) {
                console.warn('HarmonyPlayer init error:', e);
            }
        }

        _createAudioEl() {
            const el = document.createElement('audio');
            el.id = 'hidden-audio';
            el.style.display = 'none';
            el.preload = 'metadata';
            document.body.appendChild(el);
            return el;
        }

        _getRefs() {
            this.playPauseBtn = document.getElementById('play-pause-btn');
            this.trackTitleEl  = document.getElementById('track-title');
            this.trackArtistEl = document.getElementById('track-artist');
            this.currentTimeEl = document.getElementById('current-time');
            this.totalTimeEl   = document.getElementById('total-time');
            this.progressBar   = document.getElementById('custom-progress-bar');
            this.progressFill  = document.getElementById('progress-fill');
            this.progressHandle= document.getElementById('progress-handle');
            this.volumeControl = document.getElementById('custom-volume-slider');
            this.volumeFill    = document.getElementById('volume-fill');
            this.volumeHandle  = document.getElementById('volume-handle');
        }

        _buildPlaylist() {
            this.playlist = [];
            this.originalPlaylist = [];
            document.querySelectorAll(this.PLAY_SEL).forEach((btn, i) => {
                const t = this._extractTrack(btn);
                if (t.url && t.url.trim() && t.url !== 'null') {
                    t.index = i;
                    this.playlist.push(t);
                    this.originalPlaylist.push({ ...t });
                }
            });
        }

        _extractTrack(btn) {
            return {
                id:     btn.getAttribute('data-content-id') || '',
                title:  btn.getAttribute('data-title')      || 'Sin título',
                artist: btn.getAttribute('data-artist')     || '',
                url:    btn.getAttribute('data-url')        || ''
            };
        }

        // ── Eventos ────────────────────────────────────────────────────────

        _bindEvents() {
            // Eventos del elemento de audio
            this.audio.addEventListener('loadedmetadata', () => this._onMeta());
            this.audio.addEventListener('timeupdate',     () => this._onTime());
            this.audio.addEventListener('ended',          () => this._onEnded());
            this.audio.addEventListener('error',          (e) => this._onError(e));

            // Controles del reproductor
            this.playPauseBtn?.addEventListener('click', () => this.togglePlayPause());
            document.getElementById('prev-btn')?.addEventListener('click',   () => this.playPrevious());
            document.getElementById('next-btn')?.addEventListener('click',   () => this.playNext());
            document.getElementById('repeat-btn')?.addEventListener('click', () => this.toggleRepeat());
            document.getElementById('shuffle-btn')?.addEventListener('click',() => this.toggleShuffle());

            // Botones de reproducir individualmente
            document.querySelectorAll(this.PLAY_SEL).forEach(btn => {
                btn.addEventListener('click', (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    const track = this._extractTrack(btn);
                    if (!track.url || !track.url.trim() || track.url === 'null') {
                        this.showToast('Esta canción no tiene audio disponible', 'error');
                        return;
                    }
                    this.playTrack(track);
                });
            });

            // CLIC EN LA FILA COMPLETA para reproducir
            document.querySelectorAll(this.ROW_SEL).forEach(row => {
                row.addEventListener('click', (e) => {
                    // Ignorar si se hizo clic dentro de un botón o menú
                    if (e.target.closest('button') ||
                        e.target.closest('.track-dropdown-menu') ||
                        e.target.closest('a')) return;
                    const playBtn = row.querySelector(this.PLAY_SEL);
                    playBtn?.click();
                });
            });

            // Botones de like en la lista
            document.querySelectorAll(this.LIKE_SEL).forEach(btn => {
                btn.addEventListener('click', (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    this.toggleLike(btn);
                });
            });

            // Corazón del reproductor
            document.getElementById('like-current-btn')?.addEventListener('click', () => {
                if (!this.currentTrack) return;
                const btn = Array.from(document.querySelectorAll(this.LIKE_SEL))
                    .find(b => b.getAttribute('data-content-id') === this.currentTrack.id);
                if (btn) {
                    this.toggleLike(btn);
                } else {
                    // Vista sin like-btn en la lista — llamada directa
                    this._toggleLikeDirect(this.currentTrack.id);
                }
            });

            // Botón "Reproducir todo"
            document.querySelectorAll('.play-all-tracks, .play-all-btn').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    if (this.playlist.length) {
                        this.currentTrackIndex = 0;
                        this.playTrack(this.playlist[0]);
                    }
                });
            });

            this._setupProgressBar();
            this._setupVolumeControl();
        }

        // ── Reproducción ───────────────────────────────────────────────────

        async playTrack(track) {
            if (!track?.url || !track.url.trim() || track.url === 'null') {
                this.showToast('Canción no disponible', 'error');
                return;
            }

            this.currentTrack = track;
            this.currentTrackIndex = this.playlist.findIndex(t => t.id === track.id);

            if (this.trackTitleEl)  this.trackTitleEl.textContent  = track.title;
            if (this.trackArtistEl) this.trackArtistEl.textContent = track.artist;
            this.playerEl?.classList.remove('d-none');

            this._updateListUI(track.id);
            this._updatePlayerHeart();

            try {
                if (this.isPremiumUser) {
                    await this._playDirect(track);
                } else {
                    await this._playWithAd(track);
                }
            } catch (err) {
                console.error('Error reproduciendo:', err);
                this.isPlaying = false;
                this._refreshPlayBtn();
            }
        }

        async _playDirect(track = this.currentTrack) {
            this.isPlayingAd = false;
            if (this.trackTitleEl)  this.trackTitleEl.textContent  = track.title;
            if (this.trackArtistEl) this.trackArtistEl.textContent = track.artist;

            await this._loadAndPlay(track.url);
            this.isPlaying = true;
            this._refreshPlayBtn();
        }

        async _playWithAd(track) {
            try {
                this.isPlayingAd = true;
                this.originalTrackToPlay = track;

                const res = await fetch(this.urls.getRandomAd || '');
                if (!res.ok) throw new Error('Sin anuncio');
                const ad = await res.json();
                if (!ad.url) throw new Error('URL de anuncio inválida');

                if (this.trackTitleEl)  this.trackTitleEl.textContent  = 'Reproduciendo anuncio...';
                if (this.trackArtistEl) this.trackArtistEl.textContent = 'Anuncio publicitario';

                this._showAdNotification(ad.duration);

                if (this.adEndedHandler) this.audio.removeEventListener('ended', this.adEndedHandler);
                this.adEndedHandler = async () => {
                    this.audio.removeEventListener('ended', this.adEndedHandler);
                    this.isPlayingAd = false;
                    this._hideAdNotification();
                    setTimeout(() => this._playDirect(this.originalTrackToPlay), 400);
                };
                this.audio.addEventListener('ended', this.adEndedHandler);

                await this._loadAndPlay(ad.url);
                this.isPlaying = true;
                this._refreshPlayBtn();
            } catch {
                this.isPlayingAd = false;
                this._hideAdNotification();
                await this._playDirect(track);
            }
        }

        async _loadAndPlay(url) {
            return new Promise((resolve, reject) => {
                const onCanPlay = async () => {
                    cleanup();
                    try { await this.audio.play(); resolve(); }
                    catch (e) { reject(e); }
                };
                const onError = () => { cleanup(); reject(new Error('Error cargando audio')); };
                const cleanup = () => {
                    this.audio.removeEventListener('canplay', onCanPlay);
                    this.audio.removeEventListener('error', onError);
                };
                this.audio.addEventListener('canplay', onCanPlay);
                this.audio.addEventListener('error', onError);
                this.audio.src = url;
                this.audio.load();
            });
        }

        async togglePlayPause() {
            if (!this.audio || !this.currentTrack) return;
            try {
                if (this.audio.paused) {
                    await this.audio.play();
                    this.isPlaying = true;
                } else {
                    this.audio.pause();
                    this.isPlaying = false;
                }
                this._refreshPlayBtn();
            } catch (e) {
                this.isPlaying = false;
                this._refreshPlayBtn();
            }
        }

        playNext() {
            if (!this.playlist.length) return;
            const next = this.isShuffling
                ? this.playlist[Math.floor(Math.random() * this.playlist.length)]
                : this.playlist[(this.currentTrackIndex + 1) % this.playlist.length];
            if (next) { this.currentTrackIndex = next.index; this.playTrack(next); }
        }

        playPrevious() {
            if (!this.playlist.length) return;
            const idx = this.currentTrackIndex > 0 ? this.currentTrackIndex - 1 : this.playlist.length - 1;
            const prev = this.playlist[idx];
            if (prev) { this.currentTrackIndex = idx; this.playTrack(prev); }
        }

        toggleRepeat() {
            this.isLooping = !this.isLooping;
            document.getElementById('repeat-btn')?.classList.toggle('active', this.isLooping);
        }

        toggleShuffle() {
            this.isShuffling = !this.isShuffling;
            document.getElementById('shuffle-btn')?.classList.toggle('active', this.isShuffling);
        }

        // ── Likes ──────────────────────────────────────────────────────────

        async _loadLikes() {
            if (!this.urls.getUserLikes) return;
            try {
                const res = await fetch(this.urls.getUserLikes);
                if (!res.ok) return;
                const raw = await res.json();
                // Handle plain array OR ASP.NET Core ReferenceHandler.Preserve format {"$values":[...]}
                const ids = Array.isArray(raw) ? raw : (raw.$values || []);
                this.likedContentIds = new Set(ids.map(id => String(id)));
                this._refreshAllLikeButtons();
            } catch (e) {
                console.warn('HarmonyPlayer: Error cargando likes', e);
            }
        }

        _refreshAllLikeButtons() {
            document.querySelectorAll(this.LIKE_SEL).forEach(btn => {
                const id = btn.getAttribute('data-content-id');
                this._setLikeBtnState(btn, this.likedContentIds.has(id));
            });
            this._updatePlayerHeart();
        }

        _setLikeBtnState(btn, liked) {
            const icon = btn.querySelector('i');
            if (liked) {
                btn.classList.add('liked');
                if (icon) icon.className = 'fas fa-heart';
                btn.title = 'Quitar de favoritos';
            } else {
                btn.classList.remove('liked');
                if (icon) icon.className = 'far fa-heart';
                btn.title = 'Me gusta';
            }
        }

        async toggleLike(btn) {
            const contentId = btn.getAttribute('data-content-id');
            const userId    = btn.getAttribute('data-user-id') || this.userId;
            if (!contentId || !userId || !this.urls.likeContent) return;

            btn.disabled = true;
            const wasLiked = this.likedContentIds.has(contentId);

            // Optimistic UI
            this._setLikeBtnState(btn, !wasLiked);
            if (wasLiked) this.likedContentIds.delete(contentId);
            else          this.likedContentIds.add(contentId);
            this._updatePlayerHeart();

            try {
                const fd = new FormData();
                fd.append('contentId', contentId);
                fd.append('userId', userId);

                const res  = await fetch(this.urls.likeContent, { method: 'POST', body: fd });
                const data = await res.json();

                if (!data.success) {
                    // Revertir si falla
                    if (wasLiked) this.likedContentIds.add(contentId);
                    else          this.likedContentIds.delete(contentId);
                    this._setLikeBtnState(btn, wasLiked);
                    this._updatePlayerHeart();
                    this.showToast('No se pudo actualizar el favorito', 'error');
                } else {
                    const msg = data.action === 'added' ? '¡Agregado a favoritos!' : 'Removido de favoritos';
                    this.showToast(msg, data.action === 'added' ? 'success' : 'info');
                }
            } catch {
                // Revertir en error de red
                if (wasLiked) this.likedContentIds.add(contentId);
                else          this.likedContentIds.delete(contentId);
                this._setLikeBtnState(btn, wasLiked);
                this._updatePlayerHeart();
                this.showToast('Error de conexión al actualizar favorito', 'error');
            } finally {
                btn.disabled = false;
            }
        }

        async _toggleLikeDirect(contentId) {
            if (!this.urls.likeContent || !this.userId) return;
            const fakBtn = { getAttribute: (a) => a === 'data-content-id' ? contentId : this.userId,
                             disabled: false, classList: { add(){}, remove(){}, has: () => false },
                             querySelector: () => null, title: '' };
            // Crear botón temporal en el player bar
            const heartBtn = document.getElementById('like-current-btn');
            if (heartBtn) {
                heartBtn.setAttribute('data-content-id', contentId);
                heartBtn.setAttribute('data-user-id', this.userId);
                this.toggleLike(heartBtn);
            }
        }

        _updatePlayerHeart() {
            const btn = document.getElementById('like-current-btn');
            if (!btn || !this.currentTrack) return;
            const liked = this.likedContentIds.has(this.currentTrack.id);
            btn.classList.toggle('liked', liked);
            const icon = btn.querySelector('i');
            if (icon) icon.className = liked ? 'fas fa-heart' : 'far fa-heart';
        }

        // ── Premium ────────────────────────────────────────────────────────

        async _checkPremium() {
            if (!this.urls.checkPremium) return;
            try {
                const res = await fetch(this.urls.checkPremium);
                if (res.ok) {
                    const data = await res.json();
                    this.isPremiumUser = !!data.isPremium;
                }
            } catch { this.isPremiumUser = false; }
        }

        // ── UI helpers ─────────────────────────────────────────────────────

        _updateListUI(currentId) {
            document.querySelectorAll(this.ROW_SEL).forEach(row => {
                row.classList.toggle('playing', row.getAttribute('data-content-id') === currentId);
            });
        }

        _refreshPlayBtn() {
            if (!this.playPauseBtn) return;
            const icon = this.playPauseBtn.querySelector('i');
            if (icon) icon.className = this.isPlaying ? 'fas fa-pause' : 'fas fa-play';
        }

        // ── Barra de progreso ──────────────────────────────────────────────

        _setupProgressBar() {
            if (!this.progressBar) return;
            this.progressBar.addEventListener('mousedown', (e) => {
                if (this.isPlayingAd) return;
                this.isProgressDragging = true;
                this.progressBar.classList.add('dragging');
                this._seek(e);
            });
            document.addEventListener('mousemove', (e) => {
                if (this.isProgressDragging && !this.isPlayingAd) this._seek(e);
            });
            document.addEventListener('mouseup', () => {
                if (this.isProgressDragging) {
                    this.isProgressDragging = false;
                    this.progressBar.classList.remove('dragging');
                }
            });
        }

        _seek(e) {
            if (!this.audio?.duration || !this.progressBar) return;
            const rect = this.progressBar.getBoundingClientRect();
            const pct = Math.max(0, Math.min((e.clientX - rect.left) / rect.width, 1));
            this.audio.currentTime = pct * this.audio.duration;
            this._updateProgress();
        }

        _setupVolumeControl() {
            if (!this.volumeControl) return;
            this.volumeControl.addEventListener('mousedown', (e) => {
                this.isVolumeDragging = true;
                this.volumeControl.classList.add('dragging');
                this._setVolFromPos(e);
            });
            document.addEventListener('mousemove', (e) => {
                if (this.isVolumeDragging) this._setVolFromPos(e);
            });
            document.addEventListener('mouseup', () => {
                if (this.isVolumeDragging) {
                    this.isVolumeDragging = false;
                    this.volumeControl.classList.remove('dragging');
                }
            });
        }

        _setVolFromPos(e) {
            if (!this.volumeControl) return;
            const rect = this.volumeControl.getBoundingClientRect();
            this.setVolume(Math.max(0, Math.min((e.clientX - rect.left) / rect.width, 1)));
        }

        setVolume(vol) {
            this.volume = Math.max(0, Math.min(1, vol));
            if (this.audio) this.audio.volume = this.volume;
            if (this.volumeFill)   this.volumeFill.style.width  = (this.volume * 100) + '%';
            if (this.volumeHandle) this.volumeHandle.style.left = (this.volume * 100) + '%';
        }

        // ── Audio event handlers ───────────────────────────────────────────

        _onMeta() {
            if (this.totalTimeEl) this.totalTimeEl.textContent = this._fmt(this.audio.duration);
        }

        _onTime() {
            if (this.isProgressDragging || !this.audio?.duration) return;
            this._updateProgress();
            if (this.currentTimeEl) this.currentTimeEl.textContent = this._fmt(this.audio.currentTime);
        }

        _updateProgress() {
            if (!this.audio?.duration) return;
            const pct = (this.audio.currentTime / this.audio.duration) * 100;
            if (this.progressFill)   this.progressFill.style.width  = pct + '%';
            if (this.progressHandle) this.progressHandle.style.left = pct + '%';
        }

        _onEnded() {
            this.isPlaying = false;
            this._refreshPlayBtn();
            if (this.isPlayingAd) return;
            if (this.isLooping) {
                setTimeout(() => this.playTrack(this.currentTrack), 300);
            } else {
                setTimeout(() => this.playNext(), 800);
            }
        }

        _onError() {
            this.showToast('Error al cargar el audio', 'error');
            this.isPlaying = false;
            this._refreshPlayBtn();
        }

        _fmt(s) {
            if (!s || isNaN(s)) return '0:00';
            return `${Math.floor(s / 60)}:${String(Math.floor(s % 60)).padStart(2, '0')}`;
        }

        // ── Anuncio ────────────────────────────────────────────────────────

        _showAdNotification(duration) {
            this._hideAdNotification();
            this.adNotification = document.createElement('div');
            this.adNotification.className = 'harmony-ad-notification';
            this.adNotification.innerHTML = `
                <i class="fas fa-ad"></i>
                <strong>Reproduciendo anuncio...</strong>
                <small>No se puede adelantar</small>
                <span class="ad-countdown">${duration}s</span>`;
            document.body.appendChild(this.adNotification);

            let t = duration;
            this.adCountdownInterval = setInterval(() => {
                t--;
                const el = this.adNotification?.querySelector('.ad-countdown');
                if (el) el.textContent = t + 's';
                if (t <= 0) this._hideAdNotification();
            }, 1000);
        }

        _hideAdNotification() {
            clearInterval(this.adCountdownInterval);
            this.adCountdownInterval = null;
            this.adNotification?.remove();
            this.adNotification = null;
        }

        // ── Toast ──────────────────────────────────────────────────────────

        showToast(msg, type = 'info') {
            const colors = { success: '#e50914', error: '#e50914', info: '#3b82f6' };
            const t = document.createElement('div');
            t.className = 'harmony-toast';
            t.style.cssText = `background:${colors[type] || colors.info}`;
            t.textContent = msg;
            document.body.appendChild(t);
            // Forzar reflow para la animación
            t.offsetHeight;
            t.classList.add('show');
            setTimeout(() => { t.classList.remove('show'); setTimeout(() => t.remove(), 300); }, 2800);
        }
    }

    document.addEventListener('DOMContentLoaded', () => {
        window.harmonyPlayer = new HarmonyPlayer();
    });

})();
