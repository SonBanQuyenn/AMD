import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    host: true, // để nghe trên 0.0.0.0 - cần thiết khi chạy trong Docker container
  },
});
