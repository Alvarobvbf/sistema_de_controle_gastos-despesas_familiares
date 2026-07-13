import { NavLink, Outlet } from 'react-router'

/**
 * Layout raiz: barra de navegação fixa entre as 3 telas + área de conteúdo (Outlet).
 * NavLink aplica a classe "ativo" automaticamente na rota atual, sem lógica manual.
 */
const linkBase =
  'rounded-md px-3 py-2 text-sm font-medium transition-colors hover:bg-slate-200 dark:hover:bg-slate-700'
const linkAtivo = 'bg-slate-900 text-white hover:bg-slate-900 dark:bg-slate-100 dark:text-slate-900'

export function Layout() {
  return (
    <div className="min-h-screen">
      <header className="border-b border-slate-200 dark:border-slate-800">
        <div className="mx-auto flex max-w-4xl items-center gap-2 px-4 py-3">
          <span className="mr-4 text-lg font-semibold">OrçaLar</span>
          <nav className="flex gap-1">
            <NavLink
              to="/pessoas"
              className={({ isActive }) => `${linkBase} ${isActive ? linkAtivo : ''}`}
            >
              Pessoas
            </NavLink>
            <NavLink
              to="/transacoes"
              className={({ isActive }) => `${linkBase} ${isActive ? linkAtivo : ''}`}
            >
              Transações
            </NavLink>
            <NavLink
              to="/fixas"
              className={({ isActive }) => `${linkBase} ${isActive ? linkAtivo : ''}`}
            >
              Fixas
            </NavLink>
            <NavLink
              to="/totais"
              className={({ isActive }) => `${linkBase} ${isActive ? linkAtivo : ''}`}
            >
              Totais
            </NavLink>
          </nav>
        </div>
      </header>
      <main className="mx-auto max-w-4xl px-4 py-6">
        <Outlet />
      </main>
    </div>
  )
}
