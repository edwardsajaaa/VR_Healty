# Setup Interaksi Kursi (Duduk & Berdiri) - VR Healthy

## Fitur
✅ **Duduk Otomatis atau dengan Tombol/Tap**: Player bisa otomatis duduk begitu masuk area kursi (Box Collider Trigger), atau duduk dengan menekan tombol A / tap layar saat berada di dekat kursi.  
✅ **Kunci Pergerakan (`LockMovement`)**: Karakter otomatis berhenti dan tidak bisa berjalan saat sedang duduk di kursi.  
✅ **Posisi & Rotasi Presisi**: Teleport ke titik duduk (`sitPoint`) yang bisa diatur arah pandangannya (menghadap ke meja/depan).  
✅ **Berdiri Kembali (`UnlockMovement`)**: Terdapat instruksi UI floating yang menginformasikan player cara untuk berdiri kembali (Tekan tombol B / Spasi / Klik layar) dan kembali ke posisi berdiri dengan aman.  
✅ **Kompatibel VR, Gamepad, Keyboard & Touchscreen Android**.

---

## Cara Setup di Unity

### 1. **Pilih Objek Kursi di Scene**
   - Di panel **Hierarchy**, cari dan klik GameObject kursi dokter / kursi pasien yang ingin bisa diduduki.

### 2. **Tambahkan Box Collider (Penting untuk Area Kursi)**
   - Di **Inspector** → klik **Add Component** → ketik **Box Collider**.
   - ✅ **Centang `Is Trigger`** pada Box Collider tersebut.
   - Atur ukuran (**Size**) dan posisi (**Center**) collider agar mencakup area di sekitar kursi tempat player berdiri dan mendekat (misal `Size: X=1.5, Y=2, Z=1.5`).

### 3. **Buat Titik Duduk (`SitPoint`) & Berdiri (`StandPoint`) (Opsional tapi Sangat Disarankan)**
   Agar posisi duduk karakter pas (ketinggian mata kamera pas di atas dudukan kursi dan menghadap ke arah yang benar):
   1. Klik kanan pada GameObject Kursi di Hierarchy → **Create Empty**. Beri nama **`SitPoint`**.
   2. Posisikan `SitPoint` di atas bantalan kursi pada ketinggian mata player saat duduk (sekitar 1 meter di atas dudukan kursi).
   3. Putar rotasi `SitPoint` (sumbu Y) agar panah biru (**Forward / Z-Axis**) menghadap ke depan (ke arah meja dokter).
   4. *(Opsional)* Buat satu lagi anak GameObject bernama **`StandPoint`** dan posisikan di samping atau di depan kursi sebagai tempat berdiri saat player keluar dari kursi.

### 4. **Pasang Script `ChairSitController`**
   - Klik GameObject Kursi → **Add Component** → cari `ChairSitController`.
   - Atur parameter di Inspector:
     ```
     Chair Settings:
     - Chair Name: Nama kursi (contoh: "Kursi Dokter" atau "Kursi Tunggu")
     - Sit Point: Drag & drop GameObject 'SitPoint' dari Hierarchy ke sini
     - Stand Point: Drag & drop GameObject 'StandPoint' dari Hierarchy ke sini (jika dikosongkan, player akan kembali ke posisi sebelum duduk)
     - Sit Offset: (0, 0.4, 0) - Digunakan jika Sit Point tidak diisi

     Interaction Settings:
     - Auto Sit On Trigger Enter: 
       • Jika DICENTANG (✓): Karakter langsung otomatis duduk begitu berjalan masuk ke dalam Box Collider kursi.
       • Jika TIDAK DICENTANG ( ): Muncul petunjuk UI "Tekan 'A' / Tap untuk Duduk" saat player masuk ke dalam Box Collider.
     - Interaction Distance: 2.5 (jarak maksimal deteksi jika menggunakan tatapan / Gaze)
     ```

---

## Alur Kerja Sistem (How It Works)

1. **Masuk Area Kursi (`OnTriggerEnter`)**:
   Saat player yang memiliki tag `"Player"` (atau komponen `VRWalkController`) menyentuh Box Collider kursi (`Is Trigger = true`):
   - Jika `Auto Sit On Trigger Enter` aktif $\rightarrow$ Karakter langsung dipindahkan ke `SitPoint` dan `VRWalkController.LockMovement()` dipanggil.
   - Jika tidak $\rightarrow$ UI Hint muncul memberi tahu player untuk menekan tombol / mengetuk layar untuk duduk.

2. **Saat Duduk (`isSitting = true`)**:
   - `CharacterController` dimatikan sesaat sebelum teleport posisi agar tidak bentrok dengan fisika Unity, lalu diaktifkan kembali.
   - `VRWalkController.LockMovement()` mengunci input jalan (`isWalking = false` dan `isMovementLocked = true`), sehingga joystick / WASD tidak bisa menggerakkan karakter.
   - UI floating kecil muncul di depan kamera: *"Anda sedang duduk. Tekan 'B' / Spasi / Klik untuk Berdiri"*.

3. **Berdiri (`StandUp`)**:
   - Saat player menekan tombol B (atau tombol sekunder VR / klik / Spasi):
   - Karakter dipindahkan ke `StandPoint` (atau posisi berdiri semula sebelum duduk).
   - `VRWalkController.UnlockMovement()` dipanggil, sehingga player bebas berjalan kembali.

---

## File yang Terlibat

1. **[ChairSitController.cs](file:///c:/PROJECT%20UNITY/VR_Healty/Assets/Script/ChairSitController.cs)** — Script utama pengontrol interaksi duduk dan berdiri di kursi.
2. **[VRWalkController.cs](file:///c:/PROJECT%20UNITY/VR_Healty/Assets/Script/VRWalkController.cs)** — Mengatur penguncian dan pembukaan jalan karakter (`LockMovement()` & `UnlockMovement()`).

---

**Selamat mencoba! 🪑🎮**
