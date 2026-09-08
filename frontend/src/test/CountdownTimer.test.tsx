import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, act } from '@testing-library/react'
import CountdownTimer from '../components/CountdownTimer'

describe('CountdownTimer', () => {
  beforeEach(() => { vi.useFakeTimers() })
  afterEach(() => { vi.useRealTimers() })

  it('displays time remaining correctly for hours + minutes', () => {
    const target = new Date(Date.now() + 2 * 3_600_000 + 30 * 60_000) // 2h 30m
    render(<CountdownTimer targetTime={target} />)
    expect(screen.getByText('2h 30m')).toBeInTheDocument()
  })

  it('displays minutes and seconds when under 1 hour', () => {
    const target = new Date(Date.now() + 5 * 60_000 + 30_000) // 5m 30s
    render(<CountdownTimer targetTime={target} />)
    expect(screen.getByText('5m 30s')).toBeInTheDocument()
  })

  it('displays Locked when time has passed', () => {
    const past = new Date(Date.now() - 1000)
    render(<CountdownTimer targetTime={past} />)
    expect(screen.getByText('Locked')).toBeInTheDocument()
  })

  it('renders with prefix', () => {
    const target = new Date(Date.now() + 3_600_000) // 1h
    render(<CountdownTimer targetTime={target} prefix="Locks in " />)
    expect(screen.getByText(/Locks in/)).toBeInTheDocument()
  })
})
