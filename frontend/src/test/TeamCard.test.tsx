import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import TeamCard from '../components/TeamCard'
import type { AvailableTeam } from '../types'

const baseTeam: AvailableTeam = {
  teamId: 1,
  name: 'Chiefs',
  city: 'Kansas City',
  abbreviation: 'KC',
  isUsed: false,
  isOnBye: false,
  isLocked: false,
  isSelectedThisWeek: false,
}

const withGame = (gameTimeUtc: string, lockTimeUtc: string): AvailableTeam => ({
  ...baseTeam,
  opponentName: 'Ravens',
  opponentAbbreviation: 'BAL',
  gameTimeUtc,
  lockTimeUtc,
})

describe('TeamCard', () => {
  it('renders available team with city and name', () => {
    render(<TeamCard team={baseTeam} />)
    expect(screen.getByText('Kansas City')).toBeInTheDocument()
    expect(screen.getByText('Chiefs')).toBeInTheDocument()
  })

  it('renders used team with strikethrough styling', () => {
    const usedTeam: AvailableTeam = { ...baseTeam, isUsed: true }
    render(<TeamCard team={usedTeam} />)
    const cityEl = screen.getByText('Kansas City')
    // Used teams have line-through class
    expect(cityEl.className).toContain('line-through')
  })

  it('renders bye week team with BYE WEEK label', () => {
    const byeTeam: AvailableTeam = { ...baseTeam, isOnBye: true }
    render(<TeamCard team={byeTeam} />)
    expect(screen.getByText('BYE WEEK')).toBeInTheDocument()
  })

  it('renders locked team with lock icon', () => {
    const lockedTeam: AvailableTeam = {
      ...baseTeam,
      isLocked: true,
      gameTimeUtc: new Date(Date.now() - 3_600_000).toISOString(),
      lockTimeUtc: new Date(Date.now() - 3_600_000).toISOString(),
    }
    render(<TeamCard team={lockedTeam} />)
    expect(screen.getByText('🔒')).toBeInTheDocument()
  })

  it('renders selected team with green indicator', () => {
    const selectedTeam: AvailableTeam = { ...baseTeam, isSelectedThisWeek: true }
    render(<TeamCard team={selectedTeam} />)
    expect(screen.getByText('●')).toBeInTheDocument()
  })

  it('renders won team with checkmark', () => {
    const wonTeam: AvailableTeam = { ...baseTeam, isSelectedThisWeek: true, weekResult: 'Won' }
    render(<TeamCard team={wonTeam} />)
    expect(screen.getByText('✓')).toBeInTheDocument()
  })

  it('renders lost team with X icon', () => {
    const lostTeam: AvailableTeam = { ...baseTeam, isSelectedThisWeek: true, weekResult: 'Lost' }
    render(<TeamCard team={lostTeam} />)
    expect(screen.getByText('✗')).toBeInTheDocument()
  })

  it('renders THU badge for Thursday games', () => {
    const thursdayGame = withGame(
      new Date('2025-09-05T00:20:00Z').toISOString(),
      new Date('2025-09-05T00:19:00Z').toISOString(),
    )
    render(<TeamCard team={thursdayGame} />)
    expect(screen.getByText('THU')).toBeInTheDocument()
  })
})
