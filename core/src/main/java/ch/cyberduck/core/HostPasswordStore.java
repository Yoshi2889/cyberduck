package ch.cyberduck.core;

/*
 * Copyright (c) 2002-2018 iterate GmbH. All rights reserved.
 * https://cyberduck.io/
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

import ch.cyberduck.core.exception.LocalAccessDeniedException;

public interface HostPasswordStore extends PasswordStore {
    String findLoginPassword(Host bookmark);

    String findLoginToken(Host bookmark);

    String findPrivateKeyPassphrase(Host bookmark);

    OAuthTokens findOAuthTokens(Host bookmark);

    /**
     * Adds the password to the login keychain
     *
     * @param bookmark Hostname
     * @throws LocalAccessDeniedException Failure accessing store
     * @see ch.cyberduck.core.Host#getCredentials()
     */
    void save(Host bookmark) throws LocalAccessDeniedException;

    /**
     * Delete password in login keychain if any
     *
     * @param bookmark Hostname
     * @throws LocalAccessDeniedException Failure accessing store
     */
    void delete(Host bookmark) throws LocalAccessDeniedException;
}
