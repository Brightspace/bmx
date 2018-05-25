FROM python:3

RUN useradd -ms /bin/bash bmx

ENV PATH="/home/bmx/.local/bin:${PATH}"
USER bmx
WORKDIR /home/bmx

ADD . /home/bmx

RUN pip install --user -r requirements.txt -e .

ENTRYPOINT [ "/home/bmx/entrypoint.sh" ]