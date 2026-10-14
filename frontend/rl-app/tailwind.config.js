/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{html,ts}"],
  theme: {
    extend: {
      colors: {
        // Paleta IHSS
        ihss: {
          50: 'rgb(var(--ihss-50-rgb, 246 247 251) / <alpha-value>)',
          100: 'rgb(var(--ihss-100-rgb, 230 233 242) / <alpha-value>)',
          200: 'rgb(var(--ihss-200-rgb, 204 209 225) / <alpha-value>)',
          300: 'rgb(var(--ihss-300-rgb, 158 169 201) / <alpha-value>)',
          400: 'rgb(var(--ihss-400-rgb, 112 128 177) / <alpha-value>)',
          500: 'rgb(var(--ihss-500-rgb, 76 96 158) / <alpha-value>)',
          600: 'rgb(var(--ihss-600-rgb, 45 66 145) / <alpha-value>)',
          700: 'rgb(var(--ihss-700-rgb, 30 58 138) / <alpha-value>)',
          800: 'rgb(var(--ihss-800-rgb, 26 51 121) / <alpha-value>)',
          900: 'rgb(var(--ihss-900-rgb, 23 44 104) / <alpha-value>)',
          950: 'rgb(var(--ihss-950-rgb, 18 35 83) / <alpha-value>)',
        },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
      animation: {
        'fade-in':    'fadeIn 0.3s ease-in-out',
        'slide-in':   'slideIn 0.3s ease-out',
        'pulse-slow': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
      },
      keyframes: {
        fadeIn:  { from: { opacity: '0' }, to: { opacity: '1' } },
        slideIn: { from: { transform: 'translateY(-10px)', opacity: '0' }, to: { transform: 'translateY(0)', opacity: '1' } },
      },
    },
  },
  plugins: [require('@tailwindcss/forms')],
};
