import * as React from 'react';
import { cn } from '../../lib/utils';

interface Tab {
  id: string;
  label: string;
  content: React.ReactNode;
  disabled?: boolean;
}

interface TabsProps {
  tabs: Tab[];
  defaultTab?: string;
  activeTab?: string;
  onChange?: (tabId: string) => void;
  className?: string;
}

export function Tabs({ tabs, defaultTab, activeTab, onChange, className }: TabsProps) {
  const [internalActiveTab, setInternalActiveTab] = React.useState(
    defaultTab || tabs[0]?.id || ''
  );

  // Use controlled or uncontrolled mode
  const currentTab = activeTab !== undefined ? activeTab : internalActiveTab;

  const handleTabChange = (tabId: string) => {
    if (activeTab === undefined) {
      setInternalActiveTab(tabId);
    }
    onChange?.(tabId);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    const enabledTabs = tabs.filter((t) => !t.disabled);
    const currentIndex = enabledTabs.findIndex((t) => t.id === currentTab);

    let newIndex = -1;

    switch (e.key) {
      case 'ArrowRight':
        e.preventDefault();
        newIndex = (currentIndex + 1) % enabledTabs.length;
        break;
      case 'ArrowLeft':
        e.preventDefault();
        newIndex = (currentIndex - 1 + enabledTabs.length) % enabledTabs.length;
        break;
      case 'Home':
        e.preventDefault();
        newIndex = 0;
        break;
      case 'End':
        e.preventDefault();
        newIndex = enabledTabs.length - 1;
        break;
    }

    if (newIndex >= 0) {
      handleTabChange(enabledTabs[newIndex].id);
    }
  };

  return (
    <div className={className}>
      {/* Tab list */}
      <div
        role="tablist"
        aria-label="Tabs"
        className="flex border-b border-gray-200"
      >
        {tabs.map((tab) => (
          <button
            key={tab.id}
            role="tab"
            type="button"
            id={`tab-${tab.id}`}
            aria-selected={currentTab === tab.id}
            aria-controls={`panel-${tab.id}`}
            tabIndex={currentTab === tab.id ? 0 : -1}
            disabled={tab.disabled}
            onClick={() => handleTabChange(tab.id)}
            onKeyDown={(e) => handleKeyDown(e)}
            className={cn(
              'px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-colors',
              'focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:ring-offset-2',
              currentTab === tab.id
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300',
              tab.disabled && 'opacity-50 cursor-not-allowed'
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Tab panels */}
      {tabs.map((tab) => (
        <div
          key={tab.id}
          role="tabpanel"
          id={`panel-${tab.id}`}
          aria-labelledby={`tab-${tab.id}`}
          hidden={currentTab !== tab.id}
          tabIndex={0}
          className="py-4 focus:outline-none"
        >
          {currentTab === tab.id && tab.content}
        </div>
      ))}
    </div>
  );
}

// Simple TabList and TabPanel for more custom layouts
interface TabListProps {
  children: React.ReactNode;
  className?: string;
}

export function TabList({ children, className }: TabListProps) {
  return (
    <div role="tablist" className={cn('flex border-b border-gray-200', className)}>
      {children}
    </div>
  );
}

interface TabButtonProps {
  active: boolean;
  onClick: () => void;
  disabled?: boolean;
  children: React.ReactNode;
  className?: string;
}

export function TabButton({ active, onClick, disabled, children, className }: TabButtonProps) {
  return (
    <button
      role="tab"
      type="button"
      aria-selected={active}
      disabled={disabled}
      onClick={onClick}
      className={cn(
        'px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-colors',
        'focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:ring-offset-2',
        active
          ? 'border-blue-500 text-blue-600'
          : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300',
        disabled && 'opacity-50 cursor-not-allowed',
        className
      )}
    >
      {children}
    </button>
  );
}

interface TabPanelProps {
  active: boolean;
  children: React.ReactNode;
  className?: string;
}

export function TabPanel({ active, children, className }: TabPanelProps) {
  if (!active) return null;
  return (
    <div role="tabpanel" tabIndex={0} className={cn('py-4 focus:outline-none', className)}>
      {children}
    </div>
  );
}
