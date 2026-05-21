---
title: Parallel Data Fetching with Component Composition
impact: CRITICAL
impactDescription: eliminates server-side waterfalls
tags: server, rsc, parallel-fetching, composition
---

## Parallel Data Fetching with Component Composition

React Server Components execute sequentially within a tree. Restructure with composition to parallelize data fetching.

**Incorrect (Sidebar waits for Page's fetch to complete):**

```tsx
const SomePage = () => {
  const header = await fetchHeader()
  return (
    <div>
      <div>{header}</div>
      <Sidebar />
    </div>
  )
}

export default SomePage

const Sidebar = async () => {
  const items = await fetchSidebarItems()
  return <nav>{items.map(renderItem)}</nav>
}
```

**Correct (both fetch simultaneously):**

```tsx
const Header = async () => {
  const data = await fetchHeader()
  return <div>{data}</div>
}

const Sidebar = async () => {
  const items = await fetchSidebarItems()
  return <nav>{items.map(renderItem)}</nav>
}

const SomePage = () => {
  return (
    <div>
      <Header />
      <Sidebar />
    </div>
  )
}

export default SomePage
```

**Alternative with children prop:**

```tsx
const Header = async () => {
  const data = await fetchHeader()
  return <div>{data}</div>
}

const Sidebar = async () => {
  const items = await fetchSidebarItems()
  return <nav>{items.map(renderItem)}</nav>
}

const Layout = ({ children }: { children: ReactNode }) => {
  return (
    <div>
      <Header />
      {children}
    </div>
  )
}

const SomePage = () => {
  return (
    <Layout>
      <Sidebar />
    </Layout>
  )
}

export default SomePage
```
