import { createBrowserRouter, Navigate } from 'react-router'
import { Layout } from './components/Layout'
import { PessoasPage } from './pages/PessoasPage'
import { TransacoesPage } from './pages/TransacoesPage'
import { FixasPage } from './pages/FixasPage'
import { TotaisPage } from './pages/TotaisPage'

// As 4 rotas da aplicação, todas dentro do layout com navegação.
// "/" redireciona para "/pessoas" — não existe uma home separada no escopo do desafio.
export const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [
      { index: true, element: <Navigate to="/pessoas" replace /> },
      { path: 'pessoas', element: <PessoasPage /> },
      { path: 'transacoes', element: <TransacoesPage /> },
      { path: 'fixas', element: <FixasPage /> },
      { path: 'totais', element: <TotaisPage /> },
    ],
  },
])
