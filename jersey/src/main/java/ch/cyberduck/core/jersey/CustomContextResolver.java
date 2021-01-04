package ch.cyberduck.core.jersey;

/*
 * Copyright (c) 2002-2020 iterate GmbH. All rights reserved.
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

import javax.ws.rs.ext.ContextResolver;
import java.text.DateFormat;

import com.fasterxml.jackson.databind.ObjectMapper;

public class CustomContextResolver implements ContextResolver<ObjectMapper> {

    private final ObjectMapper mapper;

    public CustomContextResolver() {
        mapper = new CustomJacksonObjectMapper();
    }

    /**
     * Set the date format for JSON (de)serialization with Date properties.
     *
     * @param dateFormat Date format
     */
    public void setDateFormat(DateFormat dateFormat) {
        mapper.setDateFormat(dateFormat);
    }

    @Override
    public ObjectMapper getContext(Class<?> type) {
        return mapper;
    }

}
