# Presentación Cyberpunk - DataMaxxer

Esta es la presentación para el proyecto final de la asignatura de **Programación de Videojuegos** (Universidad de León). Está desarrollada en **HTML/CSS/JS** utilizando el framework de diapositivas **Reveal.js**, personalizada completamente con la estética cyberpunk del juego (colores neón cian y magenta, fuentes digitales, fondo de rejilla tecnológica y simulación de terminal interactiva).

## 🚀 Cómo Visualizar la Presentación

1. Navega hasta esta carpeta: `Presentation/`
2. Haz doble clic en el archivo `index.html` para abrirlo en cualquier navegador web moderno (Chrome, Firefox, Safari, Edge).
3. **Navegación:**
   - Usa las **flechas del teclado** (Derecha/Izquierda) o la **barra espaciadora** para avanzar.
   - Presiona `F` en el teclado para activar el **modo pantalla completa**.
   - Presiona `Esc` u `O` para entrar en la vista de **vista de cuadrícula (overview)** de todas las diapositivas.

## 📄 Cómo Exportar / Imprimir a PDF

Reveal.js tiene soporte nativo para convertir la presentación a un archivo PDF de alta calidad que conserve el diseño de la siguiente manera:

1. Abre el navegador (se recomienda **Google Chrome** o **Microsoft Edge** para mayor compatibilidad de impresión).
2. Abre la presentación agregando el parámetro `?print-pdf` al final de la URL en la barra de direcciones. Por ejemplo:
   - Si la abres de forma local: `file:///ruta/a/Presentation/index.html?print-pdf`
3. Presiona `Ctrl + P` (o `Cmd + P` en Mac) para abrir el diálogo de impresión.
4. Configura los siguientes ajustes en la ventana de impresión:
   - **Destino:** Guardar como PDF (Save as PDF).
   - **Diseño / Orientación:** Horizontal (Landscape).
   - **Márgenes:** Ninguno (None).
   - **Opciones de gráficos:** Asegúrate de activar **"Gráficos de fondo" (Background graphics)** para que se imprima la rejilla cyberpunk, los brillos de neón y las imágenes del juego.
5. Haz clic en **Guardar** y elige el nombre del archivo. ¡Listo! Ya tienes tu PDF impecable listo para entregar.

## 💻 Terminal de Demo Interactiva (Última Diapositiva)

La última diapositiva cuenta con una consola interactiva en tiempo real:
- **Transmitir Datos:** Simula logs de depuración del juego en vivo en la pantalla del proyector.
- **Limpiar Log:** Reinicia la consola simulada.
- **Ejecutar en Editor:** Si tienes Unity abierto con el proyecto local y tienes activa la API local del plugin UnityMCP, al pulsar este botón intentará cambiar el editor al modo *Play* para empezar a jugar directamente desde la presentación. Si no está conectado, mostrará un mensaje indicando cómo lanzar el juego manualmente.
