# SarajevoAir Frontend

Modern Next.js frontend application for real-time air quality monitoring in Sarajevo and Bosnia & Herzegovina.

## 🚀 Features

- **Real-time Air Quality Data**: Live AQI monitoring from OpenAQ
- **Interactive Dashboard**: Clean, mobile-first design with dark/light themes
- **Pollutant Tracking**: Detailed measurements for PM2.5, PM10, O3, NO2, SO2, CO
- **Health Recommendations**: Tailored advice for different user groups
- **Progressive Web App**: Offline support and mobile app-like experience
- **Share Functionality**: Web Share API integration

## 🛠 Tech Stack

- **Framework**: Next.js 14 with App Router
- **Language**: TypeScript
- **Styling**: Tailwind CSS with custom design system
- **Data Fetching**: SWR for real-time updates
- **Charts**: Chart.js for data visualization
- **Maps**: React Leaflet for location mapping
- **Animations**: Framer Motion for smooth transitions
- **Icons**: Heroicons

## 📦 Getting Started

### Prerequisites

- Node.js 18+ 
- npm or yarn

### Installation

1. **Install dependencies**
   ```bash
   npm install
   ```

2. **Set up environment variables**
   ```bash
   cp .env.local.example .env.local
   # Edit .env.local with your configuration
   ```

3. **Start development server**
   ```bash
   npm run dev
   ```

4. **Open your browser**
   ```
   http://localhost:3000
   ```

### Available Scripts

```bash
# Development
npm run dev          # Start development server
npm run build        # Build for production
npm run start        # Start production server
npm run lint         # Run ESLint
npm run type-check   # TypeScript type checking
```

## 🏗 Project Structure

```
frontend/
├── app/                    # Next.js App Router
│   ├── globals.css        # Global styles with theme system
│   ├── layout.tsx         # Root layout component
│   ├── page.tsx          # Home page
│   └── viewport.ts       # Viewport configuration
├── components/            # Reusable UI components
│   ├── Header.tsx        # Navigation header
│   ├── LiveAqiCard.tsx   # Main AQI display
│   └── PollutantCard.tsx # Individual pollutant cards
├── lib/                  # Utilities and configuration
│   ├── api-client.ts     # Backend API client
│   └── hooks.ts          # SWR data fetching hooks
├── public/               # Static assets
└── package.json         # Dependencies and scripts
```

## 🎨 Design System

The app uses a custom CSS variable-based theme system:

- **Colors**: AQI-specific color palette matching EPA standards
- **Typography**: Clean, accessible font hierarchy
- **Components**: Consistent card-based layout
- **Dark Mode**: Automatic system preference detection

## 📱 Responsive Design

- **Mobile First**: Optimized for mobile devices
- **Breakpoints**: sm (640px), md (768px), lg (1024px), xl (1280px)
- **Touch Friendly**: Large tap targets and smooth interactions

## 🔌 API Integration

Connects to SarajevoAir backend API:

- **Live Data**: Real-time AQI and pollutant measurements
- **Historical Data**: Time-series analysis and charts
- **Health Groups**: Personalized recommendations
- **City Comparison**: Multi-location air quality comparison

## 🚀 Deployment

### Vercel (Recommended)

1. Connect your GitHub repository to Vercel
2. Set environment variables in Vercel dashboard
3. Deploy automatically on push to main branch

### Manual Deployment

```bash
# Build the application
npm run build

# Start production server
npm run start
```

## 🧪 Testing

```bash
# Run type checking
npm run type-check

# Run linting
npm run lint

# Build test (ensures production build works)
npm run build
```

## 🌐 Environment Variables

```env
# API Configuration
NEXT_PUBLIC_API_URL=http://localhost:5000/api
NEXT_PUBLIC_BASE_URL=https://sarajevoair.app

# App Configuration  
NEXT_PUBLIC_APP_NAME=SarajevoAir
NEXT_PUBLIC_APP_VERSION=1.0.0

# Development
NODE_ENV=development
NEXT_TELEMETRY_DISABLED=1
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

## 🙏 Acknowledgments

- **OpenAQ**: Air quality data source
- **Next.js Team**: Amazing React framework
- **Tailwind CSS**: Utility-first CSS framework
- **Vercel**: Deployment and hosting platform